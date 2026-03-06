// Product Images Management
var _productId = 0;
var _token = '';

function initImageUpload(productId, token) {
    _productId = productId;
    _token = token;

    var uploadArea = document.getElementById('imageUploadArea');
    var fileInput = document.getElementById('imageFileInput');

    // File input change
    fileInput.addEventListener('change', function () {
        if (this.files.length > 0) {
            uploadFiles(this.files);
            this.value = ''; // Reset
        }
    });

    // Drag and drop
    uploadArea.addEventListener('dragover', function (e) {
        e.preventDefault();
        e.stopPropagation();
        this.classList.add('dragover');
    });

    uploadArea.addEventListener('dragleave', function (e) {
        e.preventDefault();
        e.stopPropagation();
        this.classList.remove('dragover');
    });

    uploadArea.addEventListener('drop', function (e) {
        e.preventDefault();
        e.stopPropagation();
        this.classList.remove('dragover');
        if (e.dataTransfer.files.length > 0) {
            uploadFiles(e.dataTransfer.files);
        }
    });
}

function uploadFiles(files) {
    var formData = new FormData();
    formData.append('productId', _productId);

    for (var i = 0; i < files.length; i++) {
        formData.append('files', files[i]);
    }

    var progressBar = document.getElementById('uploadProgress');
    var progressBarInner = progressBar.querySelector('.progress-bar');
    progressBar.classList.remove('d-none');
    progressBarInner.style.width = '0%';

    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/ProductImages/Upload', true);
    xhr.setRequestHeader('RequestVerificationToken', _token);

    xhr.upload.onprogress = function (e) {
        if (e.lengthComputable) {
            var percent = Math.round((e.loaded / e.total) * 100);
            progressBarInner.style.width = percent + '%';
            progressBarInner.textContent = percent + '%';
        }
    };

    xhr.onload = function () {
        progressBar.classList.add('d-none');
        var messagesDiv = document.getElementById('uploadMessages');

        if (xhr.status === 200) {
            var response = JSON.parse(xhr.responseText);
            if (response.success) {
                showMessage(messagesDiv, 'success', response.message);
                // Reload the page to show new images
                setTimeout(function () { location.reload(); }, 1000);
            } else {
                showMessage(messagesDiv, 'danger', response.message);
            }

            // Show errors if any
            if (response.errors && response.errors.length > 0) {
                response.errors.forEach(function (err) {
                    showMessage(messagesDiv, 'warning', err);
                });
            }
        } else {
            showMessage(messagesDiv, 'danger', 'שגיאה בהעלאת הקבצים');
        }
    };

    xhr.onerror = function () {
        progressBar.classList.add('d-none');
        showMessage(document.getElementById('uploadMessages'), 'danger', 'שגיאה בחיבור לשרת');
    };

    xhr.send(formData);
}

function deleteSingleImage(imageId) {
    if (!confirm('האם אתה בטוח שברצונך למחוק תמונה זו?')) return;

    fetch('/ProductImages/DeleteImage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': _token
        },
        body: 'imageId=' + imageId
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        if (data.success) {
            // Remove from DOM
            var item = document.querySelector('[data-image-id="' + imageId + '"]');
            if (item) item.remove();
            showMessage(document.getElementById('uploadMessages'), 'success', data.message);
            // Reload to update primary badge
            setTimeout(function () { location.reload(); }, 800);
        } else {
            showMessage(document.getElementById('uploadMessages'), 'danger', data.message);
        }
    })
    .catch(function () {
        showMessage(document.getElementById('uploadMessages'), 'danger', 'שגיאה במחיקת התמונה');
    });
}

function deleteSelectedImages() {
    var checkboxes = document.querySelectorAll('.image-checkbox:checked');
    if (checkboxes.length === 0) return;

    if (!confirm('האם אתה בטוח שברצונך למחוק ' + checkboxes.length + ' תמונות?')) return;

    var imageIds = [];
    checkboxes.forEach(function (cb) {
        var item = cb.closest('.image-grid-item');
        if (item) imageIds.push(parseInt(item.getAttribute('data-image-id')));
    });

    fetch('/ProductImages/DeleteMultiple', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': _token
        },
        body: JSON.stringify({ imageIds: imageIds })
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        if (data.success) {
            showMessage(document.getElementById('uploadMessages'), 'success', data.message);
            setTimeout(function () { location.reload(); }, 800);
        } else {
            showMessage(document.getElementById('uploadMessages'), 'danger', data.message);
        }
    })
    .catch(function () {
        showMessage(document.getElementById('uploadMessages'), 'danger', 'שגיאה במחיקת התמונות');
    });
}

function setPrimary(productId, imageId) {
    fetch('/ProductImages/SetPrimary', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': _token
        },
        body: 'productId=' + productId + '&imageId=' + imageId
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        if (data.success) {
            showMessage(document.getElementById('uploadMessages'), 'success', data.message);
            setTimeout(function () { location.reload(); }, 800);
        } else {
            showMessage(document.getElementById('uploadMessages'), 'danger', data.message);
        }
    })
    .catch(function () {
        showMessage(document.getElementById('uploadMessages'), 'danger', 'שגיאה בעדכון התמונה הראשית');
    });
}

function updateSelectedCount() {
    var count = document.querySelectorAll('.image-checkbox:checked').length;
    var btn = document.getElementById('btnDeleteSelected');
    var span = document.getElementById('selectedCount');
    span.textContent = count;
    if (count > 0) {
        btn.classList.remove('d-none');
    } else {
        btn.classList.add('d-none');
    }
}

function openLightbox(src) {
    document.getElementById('lightboxImage').src = src;
    var modal = new bootstrap.Modal(document.getElementById('imageLightbox'));
    modal.show();
}

function showMessage(container, type, message) {
    var alert = document.createElement('div');
    alert.className = 'alert alert-' + type + ' alert-dismissible fade show';
    alert.innerHTML = message + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>';
    container.appendChild(alert);

    // Auto-dismiss after 5 seconds
    setTimeout(function () {
        if (alert.parentNode) alert.remove();
    }, 5000);
}
