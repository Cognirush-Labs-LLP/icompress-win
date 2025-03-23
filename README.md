# Mass Image Compressor - Windows 10 and Above

Mass Image Compressor is a lightweight, fast, and powerful batch image compression tool. This new version is a complete rewrite of the original project hosted at [SourceForge](https://sourceforge.net/projects/icompress/), redesigned with a modern WinUI 3 interface and built on .NET 8 for Windows 10 and 11.

> **Note:** This project will replace the original Mass Image Compressor for Windows 10/11 after community testing and stabilization. The original Mass Image Compressor (version 3.2+) will still be available for Window 7 users, or as portable image compressor due to its smaller size. 

---

## 🔧 Features

- Compress multiple folders and files in one go (including optional subfolders)
- Seamless Windows Explorer integration via the “Send To” menu
- Advanced metadata copy settings (EXIF, XMP, IPTC) with filtering (e.g., strip GPS, serial numbers)
- Full support for **Animated Images** (GIF, PNG, WebP)
- Output to modern formats like **WebP** and **AVIF**
- Regex filters for filenames and size-based exclusions
- Suffix/Prefix options and filename text replacement for output files
- Powerful Preview UI with pixel-level image comparison (`CTRL + T`)
- Robust and faster **RAW image support**
- Multiple flexible output destination modes:
  - Replace original files
  - Output to specific folder
  - Store next to original with suffix/prefix
  - Inside a `Compressed` subfolder
- Flexible resizing:
  - By percentage
  - Long edge, fixed width/height
  - Frame-based for print or responsive image sets (1x/2x/3x)

---

## 📦 Built With & Credits

This project makes use of the following libraries and tools:

- [Magick.NET](https://github.com/dlemstra/Magick.NET) – Image conversion & manipulation
- [ExifTool](https://exiftool.org) – Metadata reading and writing
- [APNG Optimizer](https://sourceforge.net/projects/apng/files/APNG_Optimizer/) – Optimization for animated PNGs
- [FFmpeg](https://ffmpeg.org) – Underlying video and animation encoding/decoding via Magick.NET
- [OptiPNG](http://optipng.sourceforge.net/) – PNG compression
- [Serilog](https://github.com/serilog/serilog) – Flexible logging infrastructure

---

## 📥 Download

Pre-built binaries will be made available once public testing is complete. For now, clone the repo and build with Visual Studio 2022+ using .NET 8 SDK. Please read [LICENSE](./LICENSE) and [NOTICE.txt](./NOTICE.txt). If you're sharing a custom build, just make sure to remove the Cognirush Labs support email or contact links, otherwise folks might email/contact me about something I didn’t build, and I won’t be able to help (or worse, give them the wrong advice). Appreciate the understanding!

---

## 👥 Community & Feedback

We're collecting feedback during this rewrite phase. Join the discussion or report issues to help stabilize the new generation of Mass Image Compressor.

---

## 📜 License

– See [LICENSE](./LICENSE) for details.

