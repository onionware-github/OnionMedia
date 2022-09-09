## To-Do

- #### App & Code
  - Add logging features
  - Decouple OS and WinUI dependent stuff, use Services and DI instead and move Models and ViewModels in an independent Core-Project
  - Add more languages
  - Add project with unit tests

- #### Converter
  - Add custom parameters to improve quality in some cases (e.g. GIF conversion)

- #### Downloader
  - Speed up searching and adding videos
  - Make the page more adaptive

- #### Known Bugs
- Time stamp when shortening a video sometimes jumps to a wrong position when changing the time in the text box
- Writing tags to a file can corrupt the file. The reason is not known yet, it happens e.g. when tags are written in a TikTok video without automatic H264 recoding.
