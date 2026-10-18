const photoFitObservers = new WeakMap();
const COVER_USER_ZOOM = 1;
const LAYOUT_GAP_CM = 0.2;

window.layoutEditor = {
    getElementSizeAtPoint: function (x, y) {
        const element = document.elementFromPoint(x, y);
        const slot = element?.closest(".slot-photo") ?? element;
        const rect = slot.getBoundingClientRect();
        return { width: rect.width, height: rect.height };
    },
    getPhotoPositionAtPoint: function (x, y) {
        const element = document.elementFromPoint(x, y);
        const slot = element?.closest(".slot-photo");
        const image = slot?.querySelector("img.photo-img");
        return {
            offsetX: Number.parseFloat(image?.dataset.renderedOffsetX) || 0,
            offsetY: Number.parseFloat(image?.dataset.renderedOffsetY) || 0,
            maxOffsetX: Number.parseFloat(image?.dataset.maxOffsetX) || 0,
            maxOffsetY: Number.parseFloat(image?.dataset.maxOffsetY) || 0
        };
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

        const fitLayoutGrid = grid => {
            const columnSizes = grid.dataset.columnCm?.split(",").map(Number.parseFloat) ?? [];
            const rowSizes = grid.dataset.rowCm?.split(",").map(Number.parseFloat) ?? [];
            if (!columnSizes.length || !rowSizes.length || [...columnSizes, ...rowSizes].some(size => !Number.isFinite(size) || size < 0))
                return;

            const style = getComputedStyle(grid);
            const availableWidth = grid.clientWidth - Number.parseFloat(style.paddingLeft) - Number.parseFloat(style.paddingRight);
            const availableHeight = grid.clientHeight - Number.parseFloat(style.paddingTop) - Number.parseFloat(style.paddingBottom);
            if (availableWidth <= 0 || availableHeight <= 0)
                return;

            const requiredWidthCm = columnSizes.reduce((total, size) => total + size, 0) + LAYOUT_GAP_CM * (columnSizes.length - 1);
            const requiredHeightCm = rowSizes.reduce((total, size) => total + size, 0) + LAYOUT_GAP_CM * (rowSizes.length - 1);
            const scale = Math.min(availableWidth / requiredWidthCm, availableHeight / requiredHeightCm);
            if (!Number.isFinite(scale) || scale <= 0)
                return;

            grid.style.gridTemplateColumns = columnSizes.map(size => `${size * scale}px`).join(" ");
            grid.style.gridTemplateRows = rowSizes.map(size => `${size * scale}px`).join(" ");
            grid.style.columnGap = `${LAYOUT_GAP_CM * scale}px`;
            grid.style.rowGap = `${LAYOUT_GAP_CM * scale}px`;

            grid.querySelectorAll(".slot-frame[data-slot-width-cm][data-slot-height-cm]").forEach(frame => {
                const widthCm = Number.parseFloat(frame.dataset.slotWidthCm);
                const heightCm = Number.parseFloat(frame.dataset.slotHeightCm);
                if (!Number.isFinite(widthCm) || !Number.isFinite(heightCm))
                    return;

                frame.style.width = `${widthCm * scale}px`;
                frame.style.height = `${heightCm * scale}px`;
            });
        };

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
            const renderedWidth = photoWidth * scale;
            const renderedHeight = photoHeight * scale;
            const maxOffsetX = Math.max(0, (renderedWidth - slotRect.width) / 2);
            const maxOffsetY = Math.max(0, (renderedHeight - slotRect.height) / 2);
            const parsedPositionX = Number.parseFloat(image.dataset.positionX);
            const parsedPositionY = Number.parseFloat(image.dataset.positionY);
            const positionX = Number.isFinite(parsedPositionX) ? parsedPositionX : 50;
            const positionY = Number.isFinite(parsedPositionY) ? parsedPositionY : 50;
            const storedOffsetX = Number.parseFloat(image.dataset.offsetX);
            const storedOffsetY = Number.parseFloat(image.dataset.offsetY);
            const requestedOffsetX = Number.isFinite(storedOffsetX) ? storedOffsetX : ((50 - positionX) / 50) * maxOffsetX;
            const requestedOffsetY = Number.isFinite(storedOffsetY) ? storedOffsetY : ((50 - positionY) / 50) * maxOffsetY;
            const offsetX = Math.max(-maxOffsetX, Math.min(maxOffsetX, requestedOffsetX));
            const offsetY = Math.max(-maxOffsetY, Math.min(maxOffsetY, requestedOffsetY));

            image.style.position = "absolute";
            image.style.maxWidth = "none";
            image.style.maxHeight = "none";
            image.style.width = `${photoWidth}px`;
            image.style.height = `${photoHeight}px`;
            image.style.left = "50%";
            image.style.top = "50%";
            image.style.right = "auto";
            image.style.bottom = "auto";
            image.style.marginLeft = `${-photoWidth / 2}px`;
            image.style.marginTop = `${-photoHeight / 2}px`;
            image.style.objectFit = "cover";
            image.style.transformOrigin = "center center";
            image.style.transform = `translate(${offsetX}px, ${offsetY}px) scale(${scale})`;
            image.dataset.renderedOffsetX = String(offsetX);
            image.dataset.renderedOffsetY = String(offsetY);
            image.dataset.maxOffsetX = String(maxOffsetX);
            image.dataset.maxOffsetY = String(maxOffsetY);
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
            for (const entry of entries) {
                if (entry.target.matches?.(".layout-page-renderer:not(.title-page)"))
                    fitLayoutGrid(entry.target);
                entry.target.querySelectorAll("img.photo-img").forEach(fitImage);
            }
        });

        const layoutGrid = root.matches?.(".layout-page-renderer:not(.title-page)")
            ? root
            : root.querySelector(".layout-page-renderer:not(.title-page)");
        if (layoutGrid) {
            fitLayoutGrid(layoutGrid);
            resizeObserver.observe(layoutGrid);
        }

        const mutationObserver = new MutationObserver(records => {
            for (const record of records) {
                if (record.type === "attributes") {
                    if (record.target.matches?.(".layout-page-renderer:not(.title-page)"))
                        fitLayoutGrid(record.target);
                    else
                        fitImage(record.target);
                }
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
            attributeFilter: ["data-user-zoom", "data-position-x", "data-position-y", "data-offset-x", "data-offset-y", "data-column-cm", "data-row-cm", "src"]
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
