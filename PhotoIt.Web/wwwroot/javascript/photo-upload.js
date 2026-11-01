(() => {
    const batchSize = 25;
    const queues = new WeakMap();

    function replaceFiles(input, files) {
        const transfer = new DataTransfer();
        for (const file of files) {
            transfer.items.add(file);
        }

        input.files = transfer.files;
    }

    function dispatchChange(input) {
        input.dispatchEvent(new CustomEvent("change", {
            bubbles: true,
            detail: { photoUploadBatch: true }
        }));
    }

    document.addEventListener("change", event => {
        const input = event.target;
        if (!(input instanceof HTMLInputElement) || !input.matches("[data-file-upload-batch]") ||
            event.detail?.photoUploadBatch) {
            return;
        }

        const files = Array.from(input.files ?? []);
        input.dataset.uploadTotal = files.length.toString();
        if (files.length > batchSize) {
            queues.set(input, { files, nextIndex: batchSize });
            replaceFiles(input, files.slice(0, batchSize));
        } else {
            queues.delete(input);
        }
    }, true);

    document.addEventListener("dragover", event => {
        const dropzone = event.target.closest?.("[data-file-dropzone]");
        if (!dropzone) {
            return;
        }

        event.preventDefault();
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
        const input = dropzone.querySelector("[data-file-upload-batch]");
        if (!input || input.disabled || !event.dataTransfer?.files?.length) {
            return;
        }

        replaceFiles(input, Array.from(event.dataTransfer.files));
        dispatchChange(input);
    });

    window.photoUpload = {
        getTotal(inputId, fallback) {
            const input = document.getElementById(inputId);
            return Number.parseInt(input?.dataset.uploadTotal ?? "", 10) || fallback;
        },
        advance(inputId) {
            const input = document.getElementById(inputId);
            const queue = input && queues.get(input);
            if (!input || !queue) {
                return false;
            }

            if (queue.nextIndex >= queue.files.length) {
                queues.delete(input);
                return false;
            }

            const nextIndex = queue.nextIndex;
            queue.nextIndex += batchSize;
            replaceFiles(input, queue.files.slice(nextIndex, queue.nextIndex));
            dispatchChange(input);
            return true;
        },
        abort(inputId) {
            const input = document.getElementById(inputId);
            if (input) {
                queues.delete(input);
                delete input.dataset.uploadTotal;
            }
        }
    };
})();
