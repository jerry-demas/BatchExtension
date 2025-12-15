FROM cbizappdev-c4erg2gbekgvfxhd.azurecr.io/library/cert:1.0.0 AS cert
 
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
 
COPY . .
 
COPY --from=cert ./*.crt /usr/local/share/ca-certificates/

RUN apt-get update \
	&& apt-get install -y ca-certificates \
	&& update-ca-certificates \
	&& apt-get install krb5-user cifs-utils ldap-utils libsasl2-modules-gssapi-mit libldap-common -y \
	&& ln -fs /usr/share/zoneinfo/America/New_York /etc/localtime \
	&& dpkg-reconfigure --frontend noninteractive tzdata \
	&& apt-get clean \
	&& mkdir -p /app/logs \
	&& chmod -R 777 /app/logs
	
USER 100000 

# Expose the port
EXPOSE 8080
ENTRYPOINT ["dotnet", "/app/CBIZ.CCH.BatchExtension.API.dll"]