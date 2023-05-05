all: build

build:
	dotnet publish -p:PublishSingleFile=true -c Release --self-contained=false -r linux-x64

build-self-contained:
	dotnet publish -p:PublishSingleFile=true -p:PublishTrimmed=true -r linux-x64 --self-contained=true -c Release

test:
	dotnet test