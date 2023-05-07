all: build

build:
	dotnet publish -p:PublishSingleFile=true -c Release --self-contained=false -r linux-x64

build-self-contained:
	dotnet publish -p:PublishSingleFile=true -p:PublishTrimmed=true -r linux-x64 --self-contained=true -c Release

publish-linux-arm64:
	dotnet publish -p:PublishSingleFile=true -p:PublishTrimmed=true -r linux-arm64 --self-contained=true -c Release

publish-win-x64:
	dotnet publish -p:PublishSingleFile=true -p:PublishTrimmed=true -r win-x64 --self-contained=true -c Release

test:
	dotnet test