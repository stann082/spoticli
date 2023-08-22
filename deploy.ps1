remove-item .\pub\ -recurse -erroraction silentlycontinue
dotnet publish .\src\main\main.csproj -c Release -o pub
copy-item -path .\pub\main.exe -destination $env:APPDATA\utils\spo.exe -verbose
