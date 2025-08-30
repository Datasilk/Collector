---
trigger: always_on
description: When we need to create/modify an API for uploading files, images, documents
---

# Media Service
* Collector.API/Services/MediaService.cs is used to process and upload images and files to Azure Blob storage
* Create a new method in MediaService.cs if you're creating a new API that require uploading and processing files, and store those files in their own folder in Azure Blob based on the API controller name (e.g. /journal-entries/screenshot.png)