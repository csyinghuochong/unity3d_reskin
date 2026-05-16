set dir=Unity2

if not exist %dir% ( md %dir%)

mklink/J E:\GitLDYQReSkin\Unity2\Assets E:\GitLDYQReSkin\Unity\Assets

mklink/J E:\GitLDYQReSkin\Unity2\ProjectSettings E:\GitLDYQReSkin\Unity\ProjectSettings

mklink/J E:\GitLDYQReSkin\Unity2\Packages E:\GitLDYQReSkin\Unity\Packages

mklink/J E:\GitLDYQReSkin\Unity2\ProjectSettings E:\GitLDYQReSkin\Unity\ProjectSettings


pause