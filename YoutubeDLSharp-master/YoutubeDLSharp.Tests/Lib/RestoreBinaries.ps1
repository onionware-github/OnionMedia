# This script downloads FFmpeg and youtube-dl for Windows.

# Download ffmpeg
if (!(Test-Path (Join-Path $PSScriptRoot "ffmpeg.exe"))) {
	echo "Downloading FFmpeg..."
	$ffmpeg_meta_url = "https://ffbinaries.com/api/v1/version/latest"
	$data = Invoke-WebRequest $ffmpeg_meta_url | ConvertFrom-Json
	echo "Found FFmpeg version: $($data.version)"
	$ffmpeg_download_url = $data.bin.'windows-64'.ffmpeg
	$ffmpeg_archive = Join-Path $PSScriptRoot "ffmpeg.zip"
	Invoke-WebRequest -Uri $ffmpeg_download_url -OutFile $ffmpeg_archive
	Expand-Archive -Path $ffmpeg_archive -DestinationPath $PSScriptRoot -Force
	Remove-Item $ffmpeg_archive
}
else {
	echo "FFmpeg already restored."
}

# Download youtube-dl
if (!(Test-Path (Join-Path $PSScriptRoot "youtube-dl.exe"))) {
	echo "Downloading youtube-dl..."
	$ytdl_url = "https://yt-dl.org/latest/youtube-dl.exe"
	$ytdl_exe = Join-Path $PSScriptRoot "youtube-dl.exe"
	Invoke-WebRequest -Uri $ytdl_url -OutFile $ytdl_exe
	echo "youtube-dl version: $(& $ytdl_exe --version)"
}
else {
	echo "youtube-dl already restored."
}

echo "Restoring binaries finished."
