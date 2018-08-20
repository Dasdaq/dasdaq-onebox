# Dasdaq Development OneBox Environment

![image](https://user-images.githubusercontent.com/2216750/44001713-6e44273e-9e69-11e8-892e-0ebbfaeb8faf.png)

## Document
[Manual](https://github.com/Dasdaq/dasdaq-onebox/wiki/Manual)

[OneNote: Getting Started](https://onedrive.live.com/view.aspx?resid=E7E2B54D72304CC0%21427&id=documents&wd=target%28Dasdaq%2FDasdaq%20OneBox.one%7C8F76E35E-AA38-44F3-99E6-B35FD2FBC048%2F%5B%E6%95%99%E7%A8%8B%5D%20%E5%9C%A8%E6%9C%AC%E5%9C%B0%E5%AE%89%E8%A3%85OneBox%7C30E26DDE-101A-464F-B7B7-CA854CB836B9%2F%29)

## Quick Start

```bash
docker pull yuko/dasdaq-onebox
docker run --rm --name dasdaq -d -p 8888:8888 -p 5500:5500 -p 3000:3000 -p 9876:9876 -v /tmp/work:/work -v /tmp/eosio/data:/mnt/dev/data -v /tmp/eosio/config:/mnt/dev/config yuko/dasdaq-onebox  /bin/bash -c "dotnet run --project /home/dasdaq_eos/agent/Dasdaq.Dev.Agent.csproj"
```
