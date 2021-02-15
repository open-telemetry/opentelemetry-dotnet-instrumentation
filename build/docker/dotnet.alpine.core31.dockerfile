FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.10

RUN apk update && apk upgrade && apk add --no-cache --update bash curl

ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh /bin/wait-for-it
RUN chmod +x /bin/wait-for-it
