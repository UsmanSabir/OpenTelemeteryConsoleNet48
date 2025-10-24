FROM alpine
RUN apk add --no-cache curl jq bash
COPY create_orgs.sh /create_orgs.sh
RUN chmod +x /create_orgs.sh
ENTRYPOINT ["/bin/sh", "-c", "/create_orgs.sh $TENANTS"]
