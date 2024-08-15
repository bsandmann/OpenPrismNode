DO $$
    DECLARE
        target_height integer := 179966;  -- Replace with the block height you want to stop at
        current_height integer;
    BEGIN
        -- Find the maximum block height to start the deletion process
        SELECT MAX("BlockHeight")
        INTO current_height
        FROM "BlockEntities";  -- Replace with your actual table name

        -- Loop to delete rows one by one
        WHILE current_height > target_height LOOP
                -- Delete the row with the current block height
                DELETE FROM "BlockEntities"  -- Replace with your actual table name
                WHERE "BlockHeight" = current_height;

                -- Move to the next lower block height
                current_height := current_height - 1;
            END LOOP;
    END $$;