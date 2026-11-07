(() => {
    const uploadInputSelector = "[data-photo-upload-input]";
    const chapterTabSelector = "[data-photo-upload-chapter-tab]";
    const dropzoneSelector = "[data-file-dropzone]";
    const maxConcurrentUploads = 8;
    let isUploading = false;

    function setUploading(uploading) {
        isUploading = uploading;

        document.querySelectorAll(uploadInputSelector).forEach(input => {
            input.disabled = uploading;
            input.dataset.photoUploadLocked = uploading ? "true" : "false";
        });

        document.querySelectorAll(chapterTabSelector).forEach(tab => {
            tab.disabled = uploading;
        });

        document.querySelectorAll(dropzoneSelector).forEach(dropzone => {
            dropzone.classList.toggle("photo-upload-disabled", uploading);
            dropzone.setAttribute("aria-disabled", uploading ? "true" : "false");
        });
    }

    function requestJson(url, body) {
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify(body)
        }).then(async response => {
            if (!response.ok) {
                const message = await response.text();
                throw new Error(message || `Upload request failed (${response.status}).`);
            }

            return response.json();
        });
    }

    function uploadFile(file, uploadUri, contentType) {
        return new Promise((resolve, reject) => {
            const request = new XMLHttpRequest();
            request.open("PUT", uploadUri);
            request.setRequestHeader("x-ms-blob-type", "BlockBlob");
            request.setRequestHeader("x-ms-version", "2023-11-03");
            request.setRequestHeader("Content-Type", contentType);
            request.onload = () => {
                if (request.status >= 200 && request.status < 300) {
                    resolve();
                } else {
                    reject(new Error(`Blob Storage rejected the upload (${request.status}).`));
                }
            };
            request.onerror = () => reject(new Error(
                "Could not reach Blob Storage. Check that CORS permits PUT requests from this site."));
            request.onabort = () => reject(new Error("Upload was interrupted."));
            request.send(file);
        });
    }

    function notifyProgress(dotnet, completed, total, activeFiles) {
        return dotnet.invokeMethodAsync("UploadProgress", completed, total, [...activeFiles].join(", "));
    }

    async function uploadBatch(input, albumId, dotnet) {
        const files = Array.from(input.files || []);
        input.value = "";
        if (files.length === 0)
            return;

        const maxFiles = Number(input.dataset.maxFiles);
        if (files.length > maxFiles) {
            await dotnet.invokeMethodAsync("UploadRejected", `Select no more than ${maxFiles} files per batch.`);
            return;
        }

        if (isUploading)
            return;

        setUploading(true);

        const jsFailures = [];
        const failedPhotoIds = new Set();
        let uploadedCount = 0;
        const activeFiles = new Set();
        try {
            await dotnet.invokeMethodAsync("UploadStarted", files.length);
            const plans = await requestJson(`/api/albums/${encodeURIComponent(albumId)}/uploads/plan`, {
                files: files.map(file => ({
                    fileName: file.name,
                    contentType: file.type || "application/octet-stream",
                    length: file.size
                }))
            });

            if (!Array.isArray(plans) || plans.length !== files.length)
                throw new Error("The server returned an invalid upload plan.");

            await notifyProgress(dotnet, uploadedCount, files.length, activeFiles);
            const queue = files.map((file, index) => ({ file, plan: plans[index] }));
            const workers = Array.from({ length: Math.min(maxConcurrentUploads, queue.length) }, async () => {
                while (queue.length > 0) {
                    const item = queue.shift();
                    if (!item)
                        return;

                    activeFiles.add(item.file.name);
                    await notifyProgress(dotnet, uploadedCount, files.length, activeFiles);
                    try {
                        await uploadFile(item.file, item.plan.uploadUri, item.plan.contentType);
                    } catch (error) {
                        failedPhotoIds.add(item.plan.photoId);
                        jsFailures.push(`${item.file.name}: ${error.message}`);
                    } finally {
                        activeFiles.delete(item.file.name);
                        if (!failedPhotoIds.has(item.plan.photoId))
                            uploadedCount++;
                        await notifyProgress(dotnet, uploadedCount, files.length, activeFiles);
                    }
                }
            });

            await Promise.all(workers);
            const completedFiles = plans
                .filter(plan => !failedPhotoIds.has(plan.photoId))
                .map(plan => ({
                    photoId: plan.photoId,
                    originalFileName: plan.originalFileName,
                    blobName: plan.blobName,
                    contentType: plan.contentType,
                    length: plan.length
                }));

            let completion = { uploadedCount: 0, failures: [] };
            if (completedFiles.length > 0) {
                completion = await requestJson(`/api/albums/${encodeURIComponent(albumId)}/uploads/complete`, {
                    files: completedFiles
                });
            }

            await dotnet.invokeMethodAsync(
                "UploadFinished",
                completion.uploadedCount,
                [...jsFailures, ...(completion.failures || [])]);
        } catch (error) {
            await dotnet.invokeMethodAsync("UploadFailed", error.message);
        } finally {
            setUploading(false);
            input.value = "";
        }
    }

    function replaceFiles(input, files) {
        const transfer = new DataTransfer();
        for (const file of files)
            transfer.items.add(file);

        input.files = transfer.files;
    }

    function initialize(input, albumId, dotnet) {
        const dropzone = input.closest(dropzoneSelector);
        const onChange = () => uploadBatch(input, albumId, dotnet);
        const onDragOver = event => {
            event.preventDefault();
            if (!isUploading)
                dropzone?.classList.add("photo-upload-dragover");
        };
        const onDragLeave = event => {
            if (dropzone && !dropzone.contains(event.relatedTarget))
                dropzone.classList.remove("photo-upload-dragover");
        };
        const onDrop = event => {
            event.preventDefault();
            dropzone?.classList.remove("photo-upload-dragover");
            if (isUploading || !event.dataTransfer?.files?.length)
                return;

            replaceFiles(input, Array.from(event.dataTransfer.files));
            onChange();
        };

        input.addEventListener("change", onChange);
        dropzone?.addEventListener("dragover", onDragOver);
        dropzone?.addEventListener("dragleave", onDragLeave);
        dropzone?.addEventListener("drop", onDrop);
        input._photoUploadHandlers = { onChange, dropzone, onDragOver, onDragLeave, onDrop };
    }

    function dispose(input) {
        const handlers = input._photoUploadHandlers;
        if (!handlers)
            return;

        input.removeEventListener("change", handlers.onChange);
        handlers.dropzone?.removeEventListener("dragover", handlers.onDragOver);
        handlers.dropzone?.removeEventListener("dragleave", handlers.onDragLeave);
        handlers.dropzone?.removeEventListener("drop", handlers.onDrop);
        delete input._photoUploadHandlers;
    }

    window.photoUpload = window.photoUpload || {};
    window.photoUpload.initialize = initialize;
    window.photoUpload.dispose = dispose;
    window.photoUpload.setUploading = setUploading;
})();
