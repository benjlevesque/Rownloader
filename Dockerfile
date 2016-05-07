FROM microsoft/aspnet:1.0.0-rc1-final-coreclr

COPY . /demo
WORKDIR /demo
RUN ["dnu", "restore"]

EXPOSE 5000
ENTRYPOINT ["dnx", "web"]