(() => {
    const uploadInputSelector = "[data-photo-upload-input]";
    const chapterTabSelector = "[data-photo-upload-chapter-tab]";
    const dropzoneSelector = "[data-file-dropzone]";
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

    window.photoUpload = window.photoUpload || {};
    window.photoUpload.setUploading = setUploading;

    document.addEventListener("change", event => {
        const input = event.target;
        if (!input.matches?.(uploadInputSelector) || !input.files?.length) {
            return;
        }

        if (input.dataset.photoUploadLocked === "true") {
            event.stopImmediatePropagation();
            return;
        }

        const maxFiles = Number(input.dataset.maxFiles);
        if (Number.isFinite(maxFiles) && input.files.length > maxFiles) {
            return;
        }

        setUploading(true);
    }, true);

    function replaceFiles(input, files) {
        const transfer = new DataTransfer();
        for (const file of files) {
            transfer.items.add(file);
        }

        input.files = transfer.files;
    }

    document.addEventListener("dragover", event => {
        const dropzone = event.target.closest?.("[data-file-dropzone]");
        if (!dropzone) {
            return;
        }

        event.preventDefault();
        if (isUploading) {
            dropzone.classList.remove("photo-upload-dragover");
            return;
        }

        dropzone.classList.add("photo-upload-dragover");
    });

    document.addEventListener("dragleave", event => {
        const dropzone = event.target.closest?.("[data-file-dropzone]");
        if (dropzone && !dropzone.contains(event.relatedTarget)) {
            dropzone.classList.remove("photo-upload-dragover");
        }
    });

    document.addEventListener("drop", event => {
        const dropzone = event.target.closest?.("[data-file-dropzone]");
        if (!dropzone) {
            return;
        }

        event.preventDefault();
        dropzone.classList.remove("photo-upload-dragover");
        if (isUploading) {
            return;
        }

        const input = dropzone.querySelector(uploadInputSelector);
        if (!input || input.disabled || !event.dataTransfer?.files?.length) {
            return;
        }

        replaceFiles(input, Array.from(event.dataTransfer.files));
        input.dispatchEvent(new Event("change", { bubbles: true }));
    });
})();
