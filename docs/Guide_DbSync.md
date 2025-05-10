# Guide: Installing OPN Using the *DbSync* for Syncing

The assumed default operation of the OPN is using the *DbSync* as its backend database. This is less for technical reasons, but the implied security and trustworthiness of the data: DbSync is using its own PostgreSQL database with all transactions from the Cardano Node being send to this database. Then OPN then queries this databse to get the relevant metadata transactions, staking addresses and other information. This is a more secure way of getting the data, as the OPN does not have to trust a third party service like Blockfrost.
 
---

## Prerequisites

- SSH or terminal access to the server. The recommended environment is **Ubuntu 24.04 LTS**. Windows Subsystem for
  Linux (WSL) may also work but has not been tested.
- Access to a PostgreSQL database with the current synced blocks from DbSync. While only the connection-string for the database is needed for the OPN to run, this usually implies a full setup of a Cardano Node and a DbSync. For instructions to set this up, refer to the documentation here: 
  - [Cardano Node](https://github.com/IntersectMBO/cardano-node)
  - [DbSync](https://github.com/IntersectMBO/cardano-db-sync)
   
  *The OPN was developed against the database structure against DbSync version 13.x*. 
   Make sure that you use compabile versions of the Cardano Node and DbSync! At the time of writing this would be for example: DbSync 13.6.04 and Cardano Node 10.1.4. You are free to use any Node version as long it is compatible with the DbSync version or a newwer dbsync version did not change the database structure in significant ways.

  *Note that the PostgreSQL database does not have to be on the same machine*. 

  While it is not required to fully sync the Cardano Node or DbSync before starting the OPN it is recommended.

- Cardano wallet (optional) – OPN can run in read-only mode without a wallet. If you want to write DIDs, you need a Cardano wallet
  instance running and accessible via HTTP. The wallet itself is not part of the OPN Docker image.
  For instructions on how to set up the Cardano Wallet, refer to the [official documentation](https://github.com/cardano-foundation/cardano-wallet).
 
---

## Installation

1. Create a `docker-compose.yml` file and paste the following contents:

```yaml
version: '3.9'
services:
  openprismnode:
    image: ghcr.io/bsandmann/openprismnode:latest
    container_name: openprismnode
    restart: always
    depends_on:
      - prismdb
      - seq
    environment:
      - AppSettings__AdminAuthorizationKey=pwAdmin!
      - AppSettings__UserAuthorizationKey=pwUser!
      - AppSettings__PrismLedger__Name=preprod
      - AppSettings__PrismLedger__PrismPostgresConnectionString=Host=prismdb;Database=prismdatabase;Username=postgres;Password=postgres
      - AppSettings__PrismLedger__DbSyncPostgresConnetionString=Host=1.2.3.4;Port=5432;Databse=cexplorer; User ID=yourName;Password=yourPassword; CommandTimeout=300
      - AppSettings__CardanoWalletApiEndpoint=Http://1.2.3.4
      - AppSettings__CardanoWalletApiEndpointPort=8090
      - AppSettings__GrpcPort=50053
      - AppSettings__ApiHttpsPort=5001
      - AppSettings__SyncDataSource__Provider=DbSync
      - Seq__ServerUrl=http://seq:5341

    # Publish ports for external access if needed
    ports:
      - "5001:5001"
      - "50053:50053"
    networks:
      - prismnet

  prismdb:
    image: postgres:15
    container_name: prismdb
  restart: always
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
      - POSTGRES_DB=prismdatabase
    expose:
      - "5432"
    volumes:
      - prismdb_data:/var/lib/postgresql/data
    networks:
      - prismnet

    seq:
      image: datalust/seq:latest
      container_name: seq
  restart: always
    environment:
      - ACCEPT_EULA=Y
    ports:
      - "5341:5341"
    volumes:
      - seq_data:/data
    networks:
      - prismnet

volumes:
  prismdb_data:
  seq_data:

networks:
  prismnet:
    driver: bridge
```

  *This example assumes that the Cardano Wallet is also setup. Usually this would be on the same server as the Cardano Node. If you want to run the OPN in read-only mode, you can simple remove the AppSettings__CardanoWalletApiEndpoint and AppSettings__CardanoWalletApiEndpointPort environment variables.*

2. Update the AppSettings__PrismLedger__PrismPostgresConnectionString with the correct connection string for the PostgreSQL
   database. 

3. Optionally: Change the AppSettings__CardanoWalletApiEndpoint and AppSettings__CardanoWalletApiEndpointPort settings according to your setup if you want to write DIDs to chain

3. Update the admin and user passwords as needed.
   > 🔐 If the OPN instance is exposed to the internet, it is **strongly recommended** to change the admin password to
   prevent unauthorized access and potential data loss.

4. Navigate to the directory containing the `docker-compose.yml` file and start the services:

    ```bash
    docker compose up -d
    ```

---

## Syncing

The synchronization process starts automatically. To monitor progress:

- Open your browser and navigate to `http://localhost:5001` (or the appropriate IP address). This will open the OPN user
  interface. ![image](./images/scr6.png) You can see the connected network as well the different endpoints. Click on
  *Swagger / OpenAPI Documentation* to get the API endpoints for reference.
- Log in using the **admin password**
- From the admin interface, you can monitor and control the sync process via UI or API. ![image](./images/scr5.png)
  You can see the current sync state and the number of blocks that are already synced. You can stop and start the sync
  process and also rollback to a previous block or epoch.
- See troubleshooting section for potential issues.
 
