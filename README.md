# Dasdaq Development OneBox Environment

```bash
# docker pull yuko/onebox-docker-image
# docker run --rm --name dasdaq -d -p 8888:8888 -p 5500:5500 -p 3000:3000 -p 9876:9876 -v /tmp/work:/work -v /tmp/eosio/data:/mnt/dev/data -v /tmp/eosio/config:/mnt/dev/config yuko/onebox-docker-image  /bin/bash -c "dotnet run --project /home/dasdaq_eos/agent/Dasdaq.Dev.Agent.csproj"
```
