#!/bin/bash
# Database backup script for DouyinContentGenerator

BACKUP_DIR="/backups"
RETENTION_DAYS=30
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/douyin_content_$DATE.sql.gz"

echo "[$(date)] Starting backup..."

if [ -z "$PGPASSWORD" ] && [ -f "/run/secrets/db_password" ]; then
    export PGPASSWORD=$(cat /run/secrets/db_password)
fi

pg_dump -h postgres -U postgres douyin_content | gzip > "$BACKUP_FILE"

if [ $? -eq 0 ]; then
    echo "[$(date)] Backup completed: $BACKUP_FILE ($(ls -lh "$BACKUP_FILE" | awk '{print $5}'))"

    # Upload to Supabase Storage
    if [ -n "$SUPABASE_URL" ] && [ -n "$SUPABASE_KEY" ]; then
        echo "[$(date)] Uploading to Supabase Storage..."
        curl -X POST "$SUPABASE_URL/storage/v1/object/backups/douyin_content_$DATE.sql.gz" \
            -H "Authorization: Bearer $SUPABASE_KEY" \
            -H "Content-Type: application/octet-stream" \
            --data-binary "@$BACKUP_FILE" && \
            echo "Upload successful" || echo "Upload failed"
    fi
else
    echo "[$(date)] Backup FAILED!"
    exit 1
fi

# Cleanup old backups
echo "[$(date)] Cleaning up backups older than $RETENTION_DAYS days..."
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +$RETENTION_DAYS -delete
echo "[$(date)] Cleanup complete. Remaining backups: $(ls "$BACKUP_DIR"/*.sql.gz 2>/dev/null | wc -l)"

echo "[$(date)] Done."
