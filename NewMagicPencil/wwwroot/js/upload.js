var MAX = 15;
var EXTS = ['jpg', 'jpeg', 'png'];
var selectedFiles = [];
var dragSrc = null;

window.addEventListener('DOMContentLoaded', function () {
    loadGalleryCount();
    initFileInput();
    initUploadForm();
    initDragDrop();
    initGalleryDropZone();
    initReplaceListener();
    initLightboxClose();
});

// ── GALLERY COUNT ON LOAD ─────────────────────────────────────────
function loadGalleryCount() {
    fetch('/Category/GalleryCount?categoryId=' + PAGE_CATEGORY_ID)
        .then(function (r) { return r.json(); })
        .then(function (d) {
            updateGalleryBadge(d.count);
        });
}

function updateGalleryBadge(count) {
    var badge = document.getElementById('galleryCountBadge');
    var footer = document.getElementById('galleryFooter');
    if (badge) badge.textContent = count;
    if (footer) footer.style.display = count > 0 ? 'block' : 'none';
}

// ── FILE INPUT & PREVIEW ──────────────────────────────────────────
function initFileInput() {
    var input = document.getElementById('imageInput');
    if (!input) return;

    input.addEventListener('change', function () {
        var files = Array.from(this.files);
        var errorDiv = document.getElementById('fileError');
        var preview = document.getElementById('previewContainer');
        var saveBtn = document.getElementById('saveBtn');

        errorDiv.style.display = 'none';
        preview.innerHTML = '';
        preview.style.display = 'none';
        selectedFiles = [];
        saveBtn.disabled = true;

        if (!files.length) return;

        if (files.length > MAX) {
            errorDiv.textContent = 'Max ' + MAX + ' images allowed. You selected ' + files.length + '.';
            errorDiv.style.display = 'block';
            this.value = '';
            return;
        }

        for (var i = 0; i < files.length; i++) {
            var ext = files[i].name.split('.').pop().toLowerCase();
            if (EXTS.indexOf(ext) === -1) {
                errorDiv.textContent = '"' + files[i].name + '" not allowed. Only JPG, JPEG, PNG.';
                errorDiv.style.display = 'block';
                this.value = '';
                return;
            }
        }

        selectedFiles = files.slice();
        preview.style.display = 'flex';
        preview.style.flexWrap = 'wrap';

        files.forEach(function (file, index) {
            var reader = new FileReader();
            reader.onload = function (e) {
                var col = document.createElement('div');
                col.className = 'col-md-3 col-sm-4 col-6';
                col.id = 'preview-card-' + index;
                col.innerHTML =
                    '<div class="card h-100 shadow-sm">' +
                    '<img src="' + e.target.result + '" class="card-img-top" style="height:140px;object-fit:cover;" />' +
                    '<div class="card-body p-2"><small class="text-muted text-break">' + file.name + '</small></div>' +
                    '<div class="card-footer bg-white d-flex justify-content-between p-2">' +
                    '<label class="btn btn-sm btn-outline-warning mb-0" style="cursor:pointer;">' +
                    '<i class="bi bi-arrow-repeat"></i>' +
                    '<input type="file" class="d-none preview-replace-input" accept=".jpg,.jpeg,.png" data-index="' + index + '" />' +
                    '</label>' +
                    '<button type="button" class="btn btn-sm btn-outline-danger" onclick="removePreview(' + index + ')">' +
                    '<i class="bi bi-trash"></i></button>' +
                    '</div></div>';
                preview.appendChild(col);

                col.querySelector('.preview-replace-input').addEventListener('change', function () {
                    var newFile = this.files[0];
                    if (!newFile) return;
                    if (EXTS.indexOf(newFile.name.split('.').pop().toLowerCase()) === -1) {
                        Swal.fire({ icon: 'error', title: 'Invalid Format', text: 'Only JPG, JPEG, PNG allowed.', confirmButtonColor: '#6c5ce7' });
                        return;
                    }
                    var idx = parseInt(this.dataset.index);
                    selectedFiles[idx] = newFile;
                    var r = new FileReader();
                    r.onload = function (ev) {
                        document.querySelector('#preview-card-' + idx + ' img').src = ev.target.result;
                        document.querySelector('#preview-card-' + idx + ' .text-muted').textContent = newFile.name;
                    };
                    r.readAsDataURL(newFile);
                    syncInput();
                });
            };
            reader.readAsDataURL(file);
        });

        saveBtn.disabled = false;
        syncInput();
    });
}

function removePreview(index) {
    selectedFiles.splice(index, 1);
    var card = document.getElementById('preview-card-' + index);
    if (card) card.remove();
    if (!selectedFiles.length) {
        document.getElementById('previewContainer').style.display = 'none';
        document.getElementById('saveBtn').disabled = true;
        document.getElementById('imageInput').value = '';
    } else { syncInput(); }
}

function syncInput() {
    try {
        var dt = new DataTransfer();
        selectedFiles.forEach(function (f) { if (f) dt.items.add(f); });
        document.getElementById('imageInput').files = dt.files;
    } catch (e) { }
}

function initUploadForm() {
    var form = document.getElementById('uploadForm');
    if (!form) return;
    form.addEventListener('submit', function () {
        if (!selectedFiles.length) return;
        var btn = document.getElementById('saveBtn');
        btn.querySelector('.btn-text').innerHTML = '<i class="bi bi-hourglass-split"></i> Saving...';
        btn.querySelector('.spinner-border').classList.remove('d-none');
        btn.disabled = true;
    });
}

// ── DRAG & DROP REORDER (within grid) ────────────────────────────
function initDragDrop() {
    var grid = document.getElementById('savedImagesGrid');
    if (!grid) return;

    grid.querySelectorAll('.drag-item').forEach(function (item) {
        item.addEventListener('dragstart', function (e) {
            dragSrc = this;
            this.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', this.dataset.id);
            e.dataTransfer.setData('imgurl', this.dataset.imgurl);
        });
        item.addEventListener('dragend', function () {
            this.classList.remove('dragging');
            grid.querySelectorAll('.drag-item').forEach(function (i) { i.classList.remove('drag-over'); });
        });
        item.addEventListener('dragover', function (e) {
            e.preventDefault();
            if (this !== dragSrc) {
                grid.querySelectorAll('.drag-item').forEach(function (i) { i.classList.remove('drag-over'); });
                this.classList.add('drag-over');
            }
        });
        item.addEventListener('dragleave', function () { this.classList.remove('drag-over'); });
        item.addEventListener('drop', function (e) {
            e.preventDefault();
            if (this === dragSrc) return;
            this.classList.remove('drag-over');
            var allItems = Array.from(grid.querySelectorAll('.drag-item'));
            var srcIdx = allItems.indexOf(dragSrc);
            var dstIdx = allItems.indexOf(this);
            if (srcIdx < dstIdx) grid.insertBefore(dragSrc, this.nextSibling);
            else grid.insertBefore(dragSrc, this);
            document.getElementById('saveOrderBtn').classList.remove('d-none');
        });
    });
}

// ── GALLERY DROP ZONE ─────────────────────────────────────────────
function initGalleryDropZone() {
    var zone = document.getElementById('galleryDropZone');
    if (!zone) return;

    zone.addEventListener('dragover', function (e) {
        e.preventDefault();
        this.classList.add('drag-over-zone');
    });
    zone.addEventListener('dragleave', function () {
        this.classList.remove('drag-over-zone');
    });
    zone.addEventListener('drop', function (e) {
        e.preventDefault();
        this.classList.remove('drag-over-zone');

        var imageId = e.dataTransfer.getData('text/plain');
        var imageUrl = e.dataTransfer.getData('imgurl');
        if (!imageId) return;

        addToGallery(parseInt(imageId), imageUrl);
    });
}

function addToGallery(categoryImageId, imageUrl) {
    var formData = new FormData();
    formData.append('categoryImageId', categoryImageId);
    formData.append('categoryId', PAGE_CATEGORY_ID);
    formData.append('__RequestVerificationToken', ANTIFORGERY_TOKEN);

    fetch('/Category/AddToGallery', { method: 'POST', body: formData })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (!data.success) {
                Swal.fire({ icon: 'error', title: 'Error', text: data.message, confirmButtonColor: '#6c5ce7' });
                return;
            }
            if (data.alreadyAdded) {
                Swal.fire({
                    icon: 'info', title: 'Already Added', text: 'This image is already in your gallery.',
                    timer: 2000, showConfirmButton: false, toast: true, position: 'top-end'
                });
                return;
            }

            // Add thumb to gallery box
            showInGalleryBox(imageUrl, data.galleryCount);

            Swal.fire({
                icon: 'success', title: 'Added to Gallery!', text: data.message,
                timer: 2000, showConfirmButton: false, toast: true, position: 'top-end'
            });
        })
        .catch(function () {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Network error.', confirmButtonColor: '#6c5ce7' });
        });
}

function showInGalleryBox(imageUrl, count) {
    var hint = document.getElementById('galleryDropHint');
    var dropped = document.getElementById('galleryDroppedImages');
    var footer = document.getElementById('galleryFooter');
    var badge = document.getElementById('galleryCountBadge');

    if (hint) hint.style.display = 'none';
    if (dropped) {
        dropped.style.removeProperty('display');
        dropped.style.display = 'flex';
        dropped.style.flexWrap = 'wrap';

        var col = document.createElement('div');
        col.className = 'col-4 p-1';
        col.innerHTML = '<img src="' + imageUrl + '" class="gallery-thumb" />';
        dropped.appendChild(col);
    }
    if (footer) footer.style.display = 'block';
    if (badge) badge.textContent = count;
}

// ── SAVE REORDER ──────────────────────────────────────────────────
async function saveOrder() {
    var grid = document.getElementById('savedImagesGrid');
    var items = Array.from(grid.querySelectorAll('.drag-item'));
    var orderData = items.map(function (item, index) {
        return { id: parseInt(item.dataset.id), sortOrder: index };
    });

    var btn = document.getElementById('saveOrderBtn');
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Saving...';
    btn.disabled = true;

    var res = await fetch('/Category/ReorderImages', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': ANTIFORGERY_TOKEN },
        body: JSON.stringify(orderData)
    });
    var data = await res.json();

    btn.innerHTML = '<i class="bi bi-save"></i> Save Order';
    btn.disabled = false;

    if (data.success) {
        btn.classList.add('d-none');
        Swal.fire({
            icon: 'success', title: 'Order Saved!', text: data.message,
            timer: 2000, showConfirmButton: false, toast: true, position: 'top-end'
        });
    } else {
        Swal.fire({ icon: 'error', title: 'Error', text: data.message, confirmButtonColor: '#6c5ce7' });
    }
}

// ── EDIT DESCRIPTION ──────────────────────────────────────────────
function openEditModal(id, description) {
    document.getElementById('editImageId').value = id;
    document.getElementById('editDescription').value = description;
    new bootstrap.Modal(document.getElementById('editModal')).show();
}

async function saveEditDescription() {
    var id = document.getElementById('editImageId').value;
    var description = document.getElementById('editDescription').value.trim();

    document.getElementById('editSaveText').classList.add('d-none');
    document.getElementById('editSaveSpinner').classList.remove('d-none');

    var formData = new FormData();
    formData.append('id', id);
    formData.append('description', description);
    formData.append('__RequestVerificationToken', ANTIFORGERY_TOKEN);

    var res = await fetch('/Category/EditImage', { method: 'POST', body: formData });
    var data = await res.json();

    document.getElementById('editSaveText').classList.remove('d-none');
    document.getElementById('editSaveSpinner').classList.add('d-none');

    if (data.success) {
        var card = document.getElementById('saved-card-' + id);
        if (card) {
            var descEl = card.querySelector('.img-desc');
            if (descEl) {
                descEl.textContent = description.length === 0 ? 'No description'
                    : description.length > 50 ? description.substring(0, 50) + '...' : description;
            }
        }
        bootstrap.Modal.getInstance(document.getElementById('editModal')).hide();
        Swal.fire({
            icon: 'success', title: 'Updated!', text: data.message,
            timer: 2000, showConfirmButton: false, toast: true, position: 'top-end'
        });
    } else {
        Swal.fire({ icon: 'error', title: 'Error', text: data.message, confirmButtonColor: '#6c5ce7' });
    }
}

// ── DELETE IMAGE ──────────────────────────────────────────────────
function deleteImage(id, categoryId) {
    Swal.fire({
        title: 'Delete Image?', text: 'This will permanently delete the image.',
        icon: 'warning', showCancelButton: true,
        confirmButtonColor: '#d33', cancelButtonColor: '#6c757d', confirmButtonText: 'Yes, Delete!'
    }).then(async function (result) {
        if (!result.isConfirmed) return;
        Swal.fire({ title: 'Deleting...', allowOutsideClick: false, didOpen: function () { Swal.showLoading(); } });

        var formData = new FormData();
        formData.append('id', id);
        formData.append('categoryId', categoryId);
        formData.append('__RequestVerificationToken', ANTIFORGERY_TOKEN);

        var res = await fetch('/Category/DeleteImage', { method: 'POST', body: formData });
        var data = await res.json();

        if (data.success) {
            var card = document.getElementById('saved-card-' + id);
            if (card) card.remove();
            var badge = document.querySelector('.card-header .badge.bg-primary');
            if (badge) {
                var count = parseInt(badge.textContent) - 1;
                if (count <= 0) location.reload();
                else badge.textContent = count;
            }
            Swal.fire({
                icon: 'success', title: 'Deleted!', text: data.message,
                timer: 2000, showConfirmButton: false, toast: true, position: 'top-end'
            });
        } else {
            Swal.fire({ icon: 'error', title: 'Error', text: data.message, confirmButtonColor: '#6c5ce7' });
        }
    });
}

// ── REPLACE IMAGE ─────────────────────────────────────────────────
function initReplaceListener() {
    document.addEventListener('change', async function (e) {
        if (!e.target.classList.contains('replace-input')) return;
        var file = e.target.files[0];
        if (!file) return;
        if (EXTS.indexOf(file.name.split('.').pop().toLowerCase()) === -1) {
            Swal.fire({ icon: 'error', title: 'Invalid Format', text: 'Only JPG, JPEG, PNG allowed.', confirmButtonColor: '#6c5ce7' });
            e.target.value = '';
            return;
        }

        var id = e.target.dataset.id;
        var categoryId = e.target.dataset.categoryid;
        Swal.fire({ title: 'Replacing...', allowOutsideClick: false, didOpen: function () { Swal.showLoading(); } });

        var formData = new FormData();
        formData.append('id', id);
        formData.append('categoryId', categoryId);
        formData.append('newImage', file);
        formData.append('__RequestVerificationToken', ANTIFORGERY_TOKEN);

        var res = await fetch('/Category/ReplaceImage', { method: 'POST', body: formData });
        var data = await res.json();

        if (data.success) {
            var card = document.getElementById('saved-card-' + id);
            if (card) {
                var reader = new FileReader();
                reader.onload = function (ev) { card.querySelector('img').src = ev.target.result; };
                reader.readAsDataURL(file);
            }
            Swal.fire({
                icon: 'success', title: 'Replaced!', text: data.message,
                timer: 2000, showConfirmButton: false, toast: true, position: 'top-end'
            });
        } else {
            Swal.fire({ icon: 'error', title: 'Error', text: data.message, confirmButtonColor: '#6c5ce7' });
        }
        e.target.value = '';
    });
}

// ── LIGHTBOX ──────────────────────────────────────────────────────
function openLightbox(src) {
    document.getElementById('lightboxImg').src = src;
    document.getElementById('lightboxDownload').href = src;
    document.getElementById('lightboxModal').style.display = 'flex';
    document.body.style.overflow = 'hidden';
}

function closeLightbox() {
    document.getElementById('lightboxModal').style.display = 'none';
    document.getElementById('lightboxImg').src = '';
    document.body.style.overflow = '';
}

function initLightboxClose() {
    var modal = document.getElementById('lightboxModal');
    if (modal) {
        modal.addEventListener('click', function (e) { if (e.target === this) closeLightbox(); });
    }
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeLightbox(); });
}