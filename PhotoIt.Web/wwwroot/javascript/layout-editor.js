const photoFitObservers = new WeakMap();
const COVER_USER_ZOOM = 1;

window.layoutEditor = {
    getElementSizeAtPoint: function (x, y) {
        const element = document.elementFromPoint(x, y);
        const slot = element?.closest(".slot-photo") ?? element;
        const rect = slot.getBoundingClientRect();
        return { width: rect.width, height: rect.height };
    },
    setPointerCaptureAtPoint: function (x, y, pointerId) {
        const element = document.elementFromPoint(x, y);
        const slot = element?.closest(".slot-photo");
        if (slot && Number.isFinite(pointerId))
            slot.setPointerCapture(pointerId);
    },
    initializePhotoFit: function (root) {
        if (!root)
            return;

        const existing = photoFitObservers.get(root);
        if (existing) {
            existing.scan(root);
            return;
        }

        const observedImages = new Map();
        const observedSlots = new Set();

        const fitImage = image => {
            const slot = image.closest(".slot-photo");
            if (!slot || image.naturalWidth <= 0 || image.naturalHeight <= 0)
                return;

            const slotRect = slot.getBoundingClientRect();
            if (slotRect.width <= 0 || slotRect.height <= 0)
                return;

            const photoWidth = image.naturalWidth;
            const photoHeight = image.naturalHeight;
            const coverScale = Math.max(
                slotRect.width / photoWidth,
                slotRect.height / photoHeight);
            const parsedUserZoom = Number.parseFloat(image.dataset.userZoom);
            const userZoom = Number.isFinite(parsedUserZoom)
                ? Math.max(COVER_USER_ZOOM, parsedUserZoom)
                : COVER_USER_ZOOM;
            const scale = coverScale * userZoom;
            const renderedWidth = Math.ceil(photoWidth * scale);
            const renderedHeight = Math.ceil(photoHeight * scale);
            const extraX = Math.max(0, renderedWidth - slotRect.width);
            const extraY = Math.max(0, renderedHeight - slotRect.height);
            const parsedPositionX = Number.parseFloat(image.dataset.positionX);
            const parsedPositionY = Number.parseFloat(image.dataset.positionY);
            const positionX = Number.isFinite(parsedPositionX) ? parsedPositionX : 50;
            const positionY = Number.isFinite(parsedPositionY) ? parsedPositionY : 50;
            const left = extraX === 0 ? (slotRect.width - renderedWidth) / 2 : -extraX * (positionX / 100);
            const top = extraY === 0 ? (slotRect.height - renderedHeight) / 2 : -extraY * (positionY / 100);

            image.style.position = "absolute";
            image.style.maxWidth = "none";
            image.style.maxHeight = "none";
            image.style.width = `${renderedWidth}px`;
            image.style.height = `${renderedHeight}px`;
            image.style.left = `${left}px`;
            image.style.top = `${top}px`;
            image.style.right = "auto";
            image.style.bottom = "auto";
            image.style.objectFit = "fill";
            image.style.transform = "none";
            image.dataset.cropX = String(extraX);
            image.dataset.cropY = String(extraY);
            image.dataset.coverScale = String(coverScale);
        };

        const unobserveImage = image => {
            const onLoad = observedImages.get(image);
            if (!onLoad)
                return;

            image.removeEventListener("load", onLoad);
            observedImages.delete(image);
        };

        const observeImage = image => {
            if (!(image instanceof HTMLImageElement) || observedImages.has(image)) {
                if (image instanceof HTMLImageElement)
                    fitImage(image);
                return;
            }

            const slot = image.closest(".slot-photo");
            const onLoad = () => fitImage(image);
            observedImages.set(image, onLoad);
            image.addEventListener("load", onLoad);
            image.draggable = false;

            if (slot && !observedSlots.has(slot)) {
                observedSlots.add(slot);
                resizeObserver.observe(slot);
            }

            if (image.complete)
                fitImage(image);
        };

        const scan = node => {
            if (node instanceof Element && node.matches("img.photo-img"))
                observeImage(node);
            node.querySelectorAll?.("img.photo-img").forEach(observeImage);
        };

        const resizeObserver = new ResizeObserver(entries => {
            for (const entry of entries)
                entry.target.querySelectorAll("img.photo-img").forEach(fitImage);
        });

        const mutationObserver = new MutationObserver(records => {
            for (const record of records) {
                if (record.type === "attributes")
                    fitImage(record.target);
                else {
                    record.addedNodes.forEach(scan);
                    record.removedNodes.forEach(node => {
                        if (node instanceof Element && node.matches("img.photo-img"))
                            unobserveImage(node);
                        node.querySelectorAll?.("img.photo-img").forEach(unobserveImage);
                    });
                }
            }
        });

        scan(root);
        mutationObserver.observe(root, {
            subtree: true,
            childList: true,
            attributes: true,
            attributeFilter: ["data-user-zoom", "data-position-x", "data-position-y", "src"]
        });

        photoFitObservers.set(root, { observedImages, observedSlots, mutationObserver, resizeObserver, scan });
    },
    disposePhotoFit: function (root) {
        const state = root && photoFitObservers.get(root);
        if (!state)
            return;

        state.mutationObserver.disconnect();
        state.resizeObserver.disconnect();
        for (const [image, onLoad] of state.observedImages)
            image.removeEventListener("load", onLoad);
        photoFitObservers.delete(root);
    }
};
