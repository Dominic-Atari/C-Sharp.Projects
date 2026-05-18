FROM mcr.microsoft.com/mssql/server:2022-latest

ENV ACCEPT_EULA=Y
ENV MSSQL_PID=Developer

# Set SA password at runtime via build-arg or compose env
ARG SA_PASSWORD=DevP@ssw0rd!
ENV SA_PASSWORD=${SA_PASSWORD}

# Copy init script
COPY --chmod=0755 run-init.sh /run-init.sh

# Optional: allow mounting /scripts to supply SQL scripts
VOLUME ["/scripts"]

EXPOSE 1433

CMD ["/bin/bash","/run-init.sh"]
FROM mcr.microsoft.com/mssql/server:2022-latest

# Accept the EULA via compose or environment at runtime
ENV ACCEPT_EULA=Y
ENV MSSQL_PID=Developer

# Copy the init script into the image (scripts are mounted at runtime)
COPY run-init.sh /run-init.sh
RUN chmod +x /run-init.sh

EXPOSE 1433

CMD ["/run-init.sh"]
