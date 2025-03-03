using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using miCompressor.core;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace miCompressor.ui.common
{
    public static class ImageThumbnailViewVisibilityManager
    {
        /// <summary>
        /// Smart mode minimizes calls to IsInView. Smart mode doesn't work for tree view as it is stupidly managed internally. 
        /// </summary>
        private static bool SmartMode = false;

        private static bool debugThisClass = true;

        // The list of views sorted by currentYLocation.
        // (We use List<T> with manual sort so we can easily merge pending additions.)
        private static List<ImageThumbnailView> thumbCollections = new List<ImageThumbnailView>();
        private static readonly object thumbCollections_lock = new object();

        // Pending new views to be merged into thumbCollections.
        private static List<ImageThumbnailView> ItemsToAdd = new List<ImageThumbnailView>();
        private static readonly object ItemsToAdd_lock = new object();

        // Number of extra elements to update in anticipation when scrolling.
        private static int numberOfElementsToForceUpdateInAnticipation = 20;

        // The current view chosen to listen for effective viewport change events.
        private static ImageThumbnailView CurrentScrollLooker;
        private static int bestGuessForVisibleElementIndex = 0;


        static ImageThumbnailViewVisibilityManager()
        {
            // Start a sanity checker to ensure we can get scroll events.
            var token = new CancellationTokenSource();
            if (!SmartMode)
                _ = SanityCheckerAsync(token.Token);
        }
        /// <summary>
        /// Check if we can reliably get scroll events from the current scroll looker.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        static async Task SanityCheckerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (CurrentScrollLooker == null || !CurrentScrollLooker.IsInView())
                {
                    FindAndSetScrollLooker();
                    Console.WriteLine("_ sanitized _");

                }
                else 
                    Console.WriteLine("_");

                await Task.Delay(2000, token); // Delay for 2 seconds
            }
        }

        /// <summary>
        /// Adds a new ImageThumbnailView. Note that this is a costly operation because
        /// we need to recalc Y positions for all items and re‑sort the entire collection.
        /// The actual work is throttled so that many rapid adds are batched together.
        /// </summary>
        public static void Add(ImageThumbnailView thumb)
        {
            lock (thumbCollections_lock)
            {
                if (thumbCollections.Contains(thumb))
                    return;
            }

            lock (ItemsToAdd_lock)
            {
                if (ItemsToAdd.Contains(thumb))
                    return;
                ItemsToAdd.Add(thumb);
            }
            // Batch update: after 100ms, recalc Y positions and re‑sort all views.
            ThrottleTask.Add(100, "ImageThumbnailViewVisibilityManager_CheckVisibilityAndUpdate", () =>
            {
                // Recalculate Y for all existing elements.
                lock (thumbCollections_lock)
                {
                    foreach (var t in thumbCollections)
                    {
                        if (t.IsInView())
                            t.UpdateContent();
                    }
                }
                // Process pending additions.
                lock (ItemsToAdd_lock)
                {
                    foreach (var t in ItemsToAdd)
                    {
                        if (t.IsInView())
                            t.UpdateContent();
                    }
                    // Merge new items into the sorted collection.
                    lock (thumbCollections_lock)
                    {
                        thumbCollections.AddRange(ItemsToAdd.Where(item => thumbCollections.Contains(item) == false));
                        // Sort by the current Y coordinate.

                        thumbCollections.RemoveAll(t => !t.IsLoaded); // clean up...

                        thumbCollections.Sort((a, b) => a.CurrentYLocation.CompareTo(b.CurrentYLocation));
                    }
                    ItemsToAdd.Clear();
                }
                FindAndSetScrollLooker();
                FindAndUpdateElementVisibility();
            }, true);
        }

        /// <summary>
        /// Removes an ImageThumbnailView. Removal is less expensive so we do it immediately.
        /// If the removed view was the scroll looker, we update the leader and unsubscribe from its scroll events.
        /// </summary>
        public static void Remove(ImageThumbnailView thumb)
        {
            lock (thumbCollections_lock)
            {
                thumbCollections.Remove(thumb);
                if (CurrentScrollLooker == thumb)
                {
                    FindAndSetScrollLooker();
                    thumb.StopLookingForScrollEvent();
                }
            }
        }

        /// <summary>
        /// Finds and sets a scroll looker (the leader that monitors scroll events).
        /// The strategy is to select any visible element.
        /// </summary>
        public static void FindAndSetScrollLooker()
        {

            // Use our helper to get any visible element.
            var visible = FastFindAnyVisibleElement();

            lock (thumbCollections_lock)
            {
                CurrentScrollLooker = visible;
                // If we found one and it is not already subscribed, subscribe to its scroll events.

                foreach (var t in thumbCollections)
                    if (t != CurrentScrollLooker)
                        t.StopLookingForScrollEvent();

                if (CurrentScrollLooker != null && !CurrentScrollLooker.ScrollLookerForOtherBrothers)
                {
                    CurrentScrollLooker.KeepLookingForScrollEven();
                } else if(CurrentScrollLooker == null)
                {
                    Debug.WriteLine("**** No Scroll Looker ****");
                }
            }
        }

        /// <summary>
        /// Throttles the update of element visibility.
        /// Finds one visible element (seed) and then crawls upward and downward updating visibility
        /// until a block of non‑visible elements is found. Also forces update for a number of elements in anticipation.
        /// </summary>
        public static void FindAndUpdateElementVisibility()
        {

            ThrottleTask.Add(30, "ImageThumbnailViewVisibilityManager_FindAndUpdateElements", () =>
            {
                if (SmartMode)
                {
                    var visibleThumb = FastFindAnyVisibleElement();
                    if (visibleThumb != null)
                    {
                        // Crawl upward (moveUpwards = true) and downward (moveUpwards = false).
                        CrawlAndUpdate(visibleThumb, moveUpwards: true);
                        CrawlAndUpdate(visibleThumb, moveUpwards: false);
                    }
                } else
                {
                    CrawlAllThumbs();
                }
                    // Also update the scroll looker in case the visible block has shifted.
                    FindAndSetScrollLooker();
            }, true);
        }

        /// <summary>
        /// Quickly finds any visible element. Ideally this would be a binary search since the list is sorted,
        /// but lacking explicit viewport bounds we use a linear search here.
        /// </summary>
        private static ImageThumbnailView FastFindAnyVisibleElement()
        {
            // We need to minimize calls to IsInView. 

            lock (thumbCollections_lock)
            {

                if (thumbCollections.Count > bestGuessForVisibleElementIndex
                    && thumbCollections[bestGuessForVisibleElementIndex].IsLoaded
                    && thumbCollections[bestGuessForVisibleElementIndex].IsInView())
                    return thumbCollections[bestGuessForVisibleElementIndex];

                thumbCollections.RemoveAll(t => !t.IsLoaded); // clean up...
                if (thumbCollections.Count == 0)
                    return null;

                int count = thumbCollections.Count;

                List<int> allSteps = new List<int> { 23, 19, 17, 13, 11, 7, 5, 3, 2, 1 };
                List<int> checkedSteps = new();

                foreach (var step in allSteps)
                {
                    var visibleThumb = FindVisible(step, checkedSteps);
                    if (visibleThumb != -1)
                    {
                        bestGuessForVisibleElementIndex = visibleThumb; //for next time.
                        return thumbCollections[visibleThumb];
                    }
                    checkedSteps.Add(step);
                }

                if (thumbCollections[0].IsInView())
                    return thumbCollections[0];

                return null;
            }
        }

        /// <summary>
        /// Get Index of visible element. 
        /// </summary>
        private static int FindVisible(int step, List<int> checkedSteps)
        {
            // First pass: check every step element 
            for (int i = step; i < thumbCollections.Count; i += step)
            {
                bool skip = false;

                foreach (var prevStep in checkedSteps)
                    if (i % prevStep == 0)
                    {
                        skip = true;
                        break;
                    }

                if (skip)
                    continue;

                if (thumbCollections[i].IsInView())
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// We crawl all thumbs and check their visibility. If found visible, we update it. This is dubm mode i.e. when Smart Mode is off.
        /// </summary>
        private static void CrawlAllThumbs()
        {
            foreach (var thumb in thumbCollections)
            {
                if (thumb.IsInView())
                {
                    thumb.UpdateContent();
                }
            }
        }

        /// <summary>
        /// Starting from a given (visible) seed, crawl in one direction updating content.
        /// For each neighbor, if its current visibility differs from its last recorded state,
        /// we call UpdateContent. Once we reach a block of non‑visible items (i.e. where both
        /// IsInView and the last checked status are false) we force update a fixed number of extra items
        /// in anticipation of imminent scrolling.
        /// </summary>
        private static void CrawlAndUpdate(ImageThumbnailView startThumb, bool moveUpwards)
        {
            int showCount = 0;
            int hideCount = 0;

            lock (thumbCollections_lock)
            {
                int index = thumbCollections.IndexOf(startThumb);
                if (index == -1)
                    return;

                int step = moveUpwards ? -1 : 1;
                int currentIndex = index + step;

                while (currentIndex >= 0 && currentIndex < thumbCollections.Count)
                {
                    var thumb = thumbCollections[currentIndex];
                    // Check if the view’s current in‐view state differs from the last check.
                    bool previousVisiblity = thumb.VisibleInLastCheck;
                    bool currentVisibility = thumb.IsInView();
                    if (currentVisibility != previousVisiblity)
                    {
                        thumb.UpdateContent();
                        if (currentVisibility)
                            showCount++;
                        else hideCount++;
                    }
                    // If we have a series of items that remain not visible,
                    // update a fixed number of additional items in anticipation.
                    if (!currentVisibility && !thumb.VisibleInLastCheck)
                    {
                        bestGuessForVisibleElementIndex = currentIndex;

                        for (int i = 0; i < numberOfElementsToForceUpdateInAnticipation; i++)
                        {
                            int anticipateIndex = currentIndex + i * step;
                            if (anticipateIndex < 0 || anticipateIndex >= thumbCollections.Count)
                                break;
                            var anticipateThumb = thumbCollections[anticipateIndex];
                            if (!anticipateThumb.ImagePopulatedInAnticipation)
                            {
                                anticipateThumb.UpdateContent(forceAddThumbnailInAnticipation: true);
                            }
                        }
                        break;
                    }
                    currentIndex += step;
                }

                Debug.WriteLineIf(debugThisClass && (showCount > 0 || hideCount > 0), $"CrawlAndUpdate > Show {showCount}, Hide {hideCount}. Total {thumbCollections.Count}. Active {thumbCollections.Where(t => t.IsLoaded).Count()}");

                Debug.WriteIf(debugThisClass, ".");
            }
        }
    }
}
