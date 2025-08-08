// Dashboard JavaScript Functions

let currentCompanyId = null;
let currentDeleteCompanyId = null;

// Tab switching functions
function showDashboard() {
    hideAllViews();
    document.getElementById('dashboard-view').style.display = 'block';
    setActiveTab('Dashboard');
    loadDashboardStats();
    loadRecentActivities(); // Add this line
}

function showCompanies() {
    hideAllViews();
    document.getElementById('companies-view').style.display = 'block';
    setActiveTab('Firmalar');
    loadCompanies();
}

function showPackages() {
    hideAllViews();
    document.getElementById('packages-view').style.display = 'block';
    setActiveTab('Paketler');
    loadPackages();
}

function showReports() {
    hideAllViews();
    document.getElementById('reports-view').style.display = 'block';
    setActiveTab('Raporlar');
}

function hideAllViews() {
    document.getElementById('dashboard-view').style.display = 'none';
    document.getElementById('companies-view').style.display = 'none';
    document.getElementById('packages-view').style.display = 'none';
    document.getElementById('reports-view').style.display = 'none';
}

function setActiveTab(activeTab) {
    // Remove active class from all nav links
    document.querySelectorAll('.nav-link').forEach(link => {
        link.classList.remove('active');
    });

    // Add active class to clicked tab
    document.querySelectorAll('.nav-link').forEach(link => {
        if (link.textContent.trim().includes(activeTab)) {
            link.classList.add('active');
        }
    });
}

// Company functions
function openCompanyModal(companyId = null) {
    currentCompanyId = companyId;

    if (companyId) {
        // Edit mode
        document.getElementById('companyModalTitle').textContent = 'Firmayı Düzenle';
        document.getElementById('uniqIdDisplay').style.display = 'block';
        loadCompanyData(companyId);
    } else {
        // Add mode
        document.getElementById('companyModalTitle').textContent = 'Yeni Firma Ekle';
        document.getElementById('uniqIdDisplay').style.display = 'none';
        resetCompanyForm();
    }

    const modal = new bootstrap.Modal(document.getElementById('companyModal'));
    modal.show();
}

function resetCompanyForm() {
    document.getElementById('companyForm').reset();
    document.getElementById('companyId').value = '';

    // Remove validation classes
    document.querySelectorAll('#companyForm .form-control').forEach(input => {
        input.classList.remove('is-valid', 'is-invalid');
    });
}

function loadCompanyData(companyId) {
    fetch(`/api/Company/${companyId}`)
        .then(response => response.json())
        .then(data => {
            if (data) {
                document.getElementById('companyId').value = data.id;
                document.getElementById('companyName').value = data.companyName;
                document.getElementById('fullName').value = data.fullName;
                document.getElementById('phoneNumber').value = data.phoneNumber;
                document.getElementById('displayUniqId').value = data.uniqId;
            }
        })
        .catch(error => {
            console.error('Error loading company data:', error);
            showNotification('Firma bilgileri yüklenirken hata oluştu!', 'error');
        });
}

function saveCompany() {
    const form = document.getElementById('companyForm');

    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return;
    }

    const saveBtn = document.getElementById('saveCompanyBtn');
    const btnText = saveBtn.querySelector('.btn-text');
    const btnLoading = saveBtn.querySelector('.btn-loading');

    // Show loading state
    btnText.classList.add('d-none');
    btnLoading.classList.remove('d-none');
    saveBtn.disabled = true;

    const formData = {
        companyName: document.getElementById('companyName').value,
        fullName: document.getElementById('fullName').value,
        phoneNumber: document.getElementById('phoneNumber').value
    };

    const url = currentCompanyId ? `/api/Company/${currentCompanyId}` : '/api/Company';
    const method = currentCompanyId ? 'PUT' : 'POST';

    fetch(url, {
        method: method,
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
    })
        .then(response => response.json())
        .then(data => {
            if (data.message) {
                showNotification(data.message, 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('companyModal'));
                modal.hide();
                loadCompanies(); // Reload companies list
            } else {
                showNotification('İşlem başarısız!', 'error');
            }
        })
        .catch(error => {
            console.error('Error saving company:', error);
            showNotification('Firma kaydedilirken hata oluştu!', 'error');
        })
        .finally(() => {
            // Hide loading state
            btnText.classList.remove('d-none');
            btnLoading.classList.add('d-none');
            saveBtn.disabled = false;
        });
}

function deleteCompany(companyId) {
    currentDeleteCompanyId = companyId;
    const modal = new bootstrap.Modal(document.getElementById('deleteCompanyModal'));
    modal.show();
}

function confirmDeleteCompany() {
    if (!currentDeleteCompanyId) return;

    fetch(`/api/Company/${currentDeleteCompanyId}`, {
        method: 'DELETE'
    })
        .then(response => response.json())
        .then(data => {
            if (data.message) {
                showNotification(data.message, 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('deleteCompanyModal'));
                modal.hide();
                loadCompanies(); // Reload companies list
            } else {
                showNotification('Silme işlemi başarısız!', 'error');
            }
        })
        .catch(error => {
            console.error('Error deleting company:', error);
            showNotification('Firma silinirken hata oluştu!', 'error');
        });
}

function loadCompanies() {
    const tableBody = document.getElementById('companiesTable');
    tableBody.innerHTML = '<tr><td colspan="7" class="text-center">Yükleniyor...</td></tr>';

    fetch('/api/Company')
        .then(response => response.json())
        .then(companies => {
            if (companies && companies.length > 0) {
                tableBody.innerHTML = companies.map(company => `
                    <tr>
                        <td><strong>${company.uniqId}</strong></td>
                        <td>${company.companyName}</td>
                        <td>${company.fullName}</td>
                        <td>${company.phoneNumber}</td>
                        <td>${company.endDate ? formatDate(company.endDate) : '-'}</td>
                        <td>
                            <span class="badge ${getStatusBadgeClass(company.status)}">
                                ${company.status}
                            </span>
                        </td>
                        <td>
                            <button class="btn btn-success-custom btn-sm me-1" onclick="assignPackage('${company.id}', '${company.companyName}', '${company.fullName}', '${company.endDate ? formatDate(company.endDate) : ''}')" title="Paket Ata">
                                <i class="fas fa-plus"></i>
                            </button>
                            <button class="btn btn-info btn-sm me-1" onclick="showPurchaseHistory('${company.id}', '${company.companyName}')" title="Geçmiş">
                                <i class="fas fa-history"></i>
                            </button>
                            <button class="btn btn-warning-custom btn-sm me-1" onclick="openCompanyModal('${company.id}')" title="Düzenle">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-danger-custom btn-sm" onclick="deleteCompany('${company.id}')" title="Sil">
                                <i class="fas fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                `).join('');
            } else {
                tableBody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">Henüz firma eklenmemiş.</td></tr>';
            }
        })
        .catch(error => {
            console.error('Error loading companies:', error);
            tableBody.innerHTML = '<tr><td colspan="7" class="text-center text-danger">Firmalar yüklenirken hata oluştu!</td></tr>';
        });
}

function loadDashboardStats() {
    // Company stats
    fetch('/api/Company/stats')
        .then(response => response.json())
        .then(stats => {
            document.getElementById('totalCompanies').textContent = stats.total || 0;
            document.getElementById('activeLicenses').textContent = stats.active || 0;
            document.getElementById('expiredLicenses').textContent = stats.expired || 0;
        })
        .catch(error => {
            console.error('Error loading company stats:', error);
        });

    // Package stats
    fetch('/api/Package/stats')
        .then(response => response.json())
        .then(stats => {
            document.getElementById('totalPackages').textContent = stats.total || 0;
        })
        .catch(error => {
            console.error('Error loading package stats:', error);
        });
}

// Utility functions
function formatDate(dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('tr-TR');
}

function getStatusBadgeClass(status) {
    switch (status) {
        case 'Aktif': return 'badge-active';
        case 'Süresi Dolmuş': return 'badge-expired';
        default: return 'badge-pending';
    }
}

function showNotification(message, type = 'info') {
    // Simple notification system - you can enhance this
    const alertClass = type === 'success' ? 'alert-success' :
        type === 'error' ? 'alert-danger' : 'alert-info';

    const notification = document.createElement('div');
    notification.className = `alert ${alertClass} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(notification);

    // Auto remove after 3 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 3000);
}

// Package functions (placeholder for now)
function openPackageModal() {
    console.log('Package modal will open');
}

function loadPackages() {
    console.log('Loading packages...');
}

// Initialize dashboard when page loads
document.addEventListener('DOMContentLoaded', function () {
    showDashboard();
});

// Package functions
let currentPackageId = null;
let currentDeletePackageId = null;

function openPackageModal(packageId = null) {
    currentPackageId = packageId;

    if (packageId) {
        // Edit mode
        document.getElementById('packageModalTitle').textContent = 'Paketi Düzenle';
        loadPackageData(packageId);
    } else {
        // Add mode
        document.getElementById('packageModalTitle').textContent = 'Yeni Paket Ekle';
        resetPackageForm();
    }

    const modal = new bootstrap.Modal(document.getElementById('packageModal'));
    modal.show();
}

function resetPackageForm() {
    document.getElementById('packageForm').reset();
    document.getElementById('packageId').value = '';

    // Remove validation classes
    document.querySelectorAll('#packageForm .form-control').forEach(input => {
        input.classList.remove('is-valid', 'is-invalid');
    });
}

function loadPackageData(packageId) {
    fetch(`/api/Package/${packageId}`)
        .then(response => response.json())
        .then(data => {
            if (data) {
                document.getElementById('packageId').value = data.id;
                document.getElementById('packageCaption').value = data.caption;
                document.getElementById('packagePrice').value = data.price;
                document.getElementById('packageMonthCount').value = data.monthCount;
                document.getElementById('packageDayCount').value = data.dayCount || '';
                document.getElementById('packageDescription').value = data.description;
            }
        })
        .catch(error => {
            console.error('Error loading package data:', error);
            showNotification('Paket bilgileri yüklenirken hata oluştu!', 'error');
        });
}

function savePackage() {
    const form = document.getElementById('packageForm');

    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return;
    }

    const saveBtn = document.getElementById('savePackageBtn');
    const btnText = saveBtn.querySelector('.btn-text');
    const btnLoading = saveBtn.querySelector('.btn-loading');

    // Show loading state
    btnText.classList.add('d-none');
    btnLoading.classList.remove('d-none');
    saveBtn.disabled = true;

    const dayCount = document.getElementById('packageDayCount').value;

    const formData = {
        caption: document.getElementById('packageCaption').value,
        price: parseFloat(document.getElementById('packagePrice').value),
        monthCount: parseInt(document.getElementById('packageMonthCount').value),
        dayCount: dayCount ? parseInt(dayCount) : null,
        description: document.getElementById('packageDescription').value
    };

    const url = currentPackageId ? `/api/Package/${currentPackageId}` : '/api/Package';
    const method = currentPackageId ? 'PUT' : 'POST';

    fetch(url, {
        method: method,
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
    })
        .then(response => response.json())
        .then(data => {
            if (data.message) {
                showNotification(data.message, 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('packageModal'));
                modal.hide();
                loadPackages(); // Reload packages list
                loadDashboardStats(); // Update stats
            } else {
                showNotification('İşlem başarısız!', 'error');
            }
        })
        .catch(error => {
            console.error('Error saving package:', error);
            showNotification('Paket kaydedilirken hata oluştu!', 'error');
        })
        .finally(() => {
            // Hide loading state
            btnText.classList.remove('d-none');
            btnLoading.classList.add('d-none');
            saveBtn.disabled = false;
        });
}

function deletePackage(packageId) {
    currentDeletePackageId = packageId;
    const modal = new bootstrap.Modal(document.getElementById('deletePackageModal'));
    modal.show();
}

function confirmDeletePackage() {
    if (!currentDeletePackageId) return;

    fetch(`/api/Package/${currentDeletePackageId}`, {
        method: 'DELETE'
    })
        .then(response => response.json())
        .then(data => {
            if (data.message) {
                showNotification(data.message, 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('deletePackageModal'));
                modal.hide();
                loadPackages(); // Reload packages list
                loadDashboardStats(); // Update stats
            } else {
                showNotification('Silme işlemi başarısız!', 'error');
            }
        })
        .catch(error => {
            console.error('Error deleting package:', error);
            showNotification('Paket silinirken hata oluştu!', 'error');
        });
}

function loadPackages() {
    const tableBody = document.getElementById('packagesTable');
    tableBody.innerHTML = '<tr><td colspan="6" class="text-center">Yükleniyor...</td></tr>';

    fetch('/api/Package')
        .then(response => response.json())
        .then(packages => {
            if (packages && packages.length > 0) {
                tableBody.innerHTML = packages.map(package => `
                    <tr>
                        <td><strong>${package.caption}</strong></td>
                        <td>${package.duration}</td>
                        <td><strong>${package.formattedPrice}</strong></td>
                        <td>${package.description}</td>
                        <td>
                            <span class="badge badge-active">Aktif</span>
                        </td>
                        <td>
                            <button class="btn btn-warning-custom btn-sm me-1" onclick="openPackageModal('${package.id}')" title="Düzenle">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-danger-custom btn-sm" onclick="deletePackage('${package.id}')" title="Sil">
                                <i class="fas fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                `).join('');
            } else {
                tableBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Henüz paket eklenmemiş.</td></tr>';
            }
        })
        .catch(error => {
            console.error('Error loading packages:', error);
            tableBody.innerHTML = '<tr><td colspan="6" class="text-center text-danger">Paketler yüklenirken hata oluştu!</td></tr>';
        });
}

// Package Assignment Functions
let assignToCompanyId = null;

function assignPackage(companyId, companyName, fullName, currentEndDate) {
    assignToCompanyId = companyId;

    document.getElementById('assignToCompanyId').value = companyId;
    document.getElementById('assignCompanyName').textContent = companyName;
    document.getElementById('assignCompanyContact').textContent = fullName;
    document.getElementById('assignCurrentEndDate').textContent = currentEndDate || 'Paket Yok';

    // Show current package status
    const statusElement = document.getElementById('currentPackageStatus');
    const statusBadge = document.getElementById('currentStatusBadge');

    if (currentEndDate && currentEndDate !== 'Paket Yok') {
        const endDate = new Date(currentEndDate.split('.').reverse().join('-'));
        const now = new Date();

        if (endDate > now) {
            statusBadge.textContent = 'Aktif Paket';
            statusBadge.className = 'badge badge-active';
        } else {
            statusBadge.textContent = 'Süresi Dolmuş';
            statusBadge.className = 'badge badge-expired';
        }
        statusElement.style.display = 'block';
    } else {
        statusElement.style.display = 'none';
    }

    // Load available packages
    loadAvailablePackages();

    // Hide package preview
    document.getElementById('packagePreview').style.display = 'none';

    const modal = new bootstrap.Modal(document.getElementById('assignPackageModal'));
    modal.show();
}

function loadAvailablePackages() {
    const selectElement = document.getElementById('selectedPackageId');
    selectElement.innerHTML = '<option value="">Paket seçiniz...</option>';

    fetch('/api/Package')
        .then(response => response.json())
        .then(packages => {
            if (packages && packages.length > 0) {
                packages.forEach(package => {
                    const option = document.createElement('option');
                    option.value = package.id;
                    option.textContent = `${package.caption} - ${package.duration} - ${package.formattedPrice}`;
                    option.dataset.packageData = JSON.stringify(package);
                    selectElement.appendChild(option);
                });
            }
        })
        .catch(error => {
            console.error('Error loading packages:', error);
            showNotification('Paketler yüklenirken hata oluştu!', 'error');
        });
}

// Package selection preview
document.addEventListener('DOMContentLoaded', function () {
    document.body.addEventListener('change', function (e) {
        if (e.target.id === 'selectedPackageId') {
            const selectedOption = e.target.selectedOptions[0];

            if (selectedOption.value) {
                const packageData = JSON.parse(selectedOption.dataset.packageData);
                showPackagePreview(packageData);
            } else {
                document.getElementById('packagePreview').style.display = 'none';
            }
        }
    });
});

function showPackagePreview(packageData) {
    document.getElementById('previewPackageName').textContent = packageData.caption;
    document.getElementById('previewDuration').textContent = packageData.duration;
    document.getElementById('previewPrice').textContent = packageData.formattedPrice;

    // Get current end date from the displayed info
    const currentEndDateText = document.getElementById('assignCurrentEndDate').textContent;
    let startDate;

    if (currentEndDateText && currentEndDateText !== '-' && currentEndDateText !== 'Paket Yok') {
        // Parse current end date and check if it's in the future
        const currentEndDate = new Date(currentEndDateText.split('.').reverse().join('-'));
        const now = new Date();

        if (currentEndDate > now) {
            // Current package is active, add to existing end date
            startDate = currentEndDate;
        } else {
            // Current package expired, start from today
            startDate = now;
        }
    } else {
        // No current package, start from today
        startDate = new Date();
    }

    // Calculate new end date
    const endDate = new Date(startDate);
    endDate.setMonth(endDate.getMonth() + packageData.monthCount);
    if (packageData.dayCount) {
        endDate.setDate(endDate.getDate() + packageData.dayCount);
    }

    document.getElementById('previewNewEndDate').textContent = formatDate(endDate.toISOString());

    // Show additional info if extending existing package
    const previewElement = document.getElementById('packagePreview');
    const existingInfo = previewElement.querySelector('.extension-info');
    if (existingInfo) {
        existingInfo.remove();
    }

    if (currentEndDateText && currentEndDateText !== '-' && currentEndDateText !== 'Paket Yok') {
        const currentEndDate = new Date(currentEndDateText.split('.').reverse().join('-'));
        const now = new Date();

        if (currentEndDate > now) {
            const extensionInfo = document.createElement('div');
            extensionInfo.className = 'alert alert-warning mt-2 extension-info';
            extensionInfo.innerHTML = `
                <i class="fas fa-info-circle"></i>
                <strong>Mevcut paket uzatılacak:</strong> 
                Kalan ${Math.ceil((currentEndDate - now) / (1000 * 60 * 60 * 24))} gün korunarak yeni paket eklenecek.
            `;
            previewElement.appendChild(extensionInfo);
        }
    }

    document.getElementById('packagePreview').style.display = 'block';
}

function confirmAssignPackage() {
    const packageId = document.getElementById('selectedPackageId').value;

    if (!packageId) {
        document.getElementById('selectedPackageId').classList.add('is-invalid');
        return;
    }

    const confirmBtn = document.getElementById('confirmAssignBtn');
    const btnText = confirmBtn.querySelector('.btn-text');
    const btnLoading = confirmBtn.querySelector('.btn-loading');

    // Show loading state
    btnText.classList.add('d-none');
    btnLoading.classList.remove('d-none');
    confirmBtn.disabled = true;

    fetch(`/api/Company/${assignToCompanyId}/assign-package`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ packageId: packageId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.message) {
                showNotification(data.message, 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('assignPackageModal'));
                modal.hide();
                loadCompanies(); // Reload companies list
                loadDashboardStats(); // Update stats
                loadRecentActivities(); // Update recent activities
            } else {
                showNotification('Paket atama başarısız!', 'error');
            }
        })
        .catch(error => {
            console.error('Error assigning package:', error);
            showNotification('Paket atanırken hata oluştu!', 'error');
        })
        .finally(() => {
            // Hide loading state
            btnText.classList.remove('d-none');
            btnLoading.classList.add('d-none');
            confirmBtn.disabled = false;
        });
}

function loadRecentActivities() {
    const tableBody = document.getElementById('recentActivities');

    fetch('/api/Company/recent-activities')
        .then(response => response.json())
        .then(activities => {
            if (activities && activities.length > 0) {
                tableBody.innerHTML = activities.map(activity => `
                    <tr>
                        <td>${formatDate(activity.createAt)}</td>
                        <td>
                            <strong>${activity.companyName}</strong><br>
                            <small class="text-muted">ID: ${activity.companyUniqId}</small>
                        </td>
                        <td>${activity.companyEndDate ? formatDate(activity.companyEndDate) : '-'}</td>
                        <td>
                            <span class="badge ${activity.isAdminAssignment ? 'badge-pending' : 'badge-active'}">
                                ${activity.assignmentType}
                            </span>
                        </td>
                        <td>
                            <span class="badge ${activity.status ? 'badge-active' : 'badge-expired'}">
                                ${activity.status ? 'Başarılı' : 'Başarısız'}
                            </span>
                        </td>
                        <td>
                            <small>${activity.description}</small>
                        </td>
                    </tr>
                `).join('');
            } else {
                tableBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Henüz aktivite bulunmamaktadır.</td></tr>';
            }
        })
        .catch(error => {
            console.error('Error loading recent activities:', error);
        });
}

function showPurchaseHistory(companyId, companyName) {
    document.getElementById('historyCompanyName').textContent = companyName + ' - Satın Alma Geçmişi';

    const tableBody = document.getElementById('purchaseHistoryTable');
    tableBody.innerHTML = '<tr><td colspan="7" class="text-center">Yükleniyor...</td></tr>';

    fetch(`/api/Company/${companyId}/purchase-history`)
        .then(response => response.json())
        .then(purchases => {
            if (purchases && purchases.length > 0) {
                tableBody.innerHTML = purchases.map(purchase => `
                    <tr>
                        <td>${formatDate(purchase.createAt)}</td>
                        <td><strong>${purchase.packageName}</strong></td>
                        <td>${purchase.duration}</td>
                        <td><strong>${purchase.packagePrice.toFixed(2)} ₺</strong></td>
                        <td>
                            <span class="badge ${purchase.isAdminAssignment ? 'badge-pending' : 'badge-active'}">
                                ${purchase.assignmentType}
                            </span>
                        </td>
                        <td>
                            <span class="badge ${purchase.status ? 'badge-active' : 'badge-expired'}">
                                ${purchase.status ? 'Başarılı' : 'Başarısız'}
                            </span>
                        </td>
                        <td><small>${purchase.description}</small></td>
                    </tr>
                `).join('');
            } else {
                tableBody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">Henüz satın alma işlemi bulunmamaktadır.</td></tr>';
            }
        })
        .catch(error => {
            console.error('Error loading purchase history:', error);
            tableBody.innerHTML = '<tr><td colspan="7" class="text-center text-danger">Satın alma geçmişi yüklenirken hata oluştu!</td></tr>';
        });

    const modal = new bootstrap.Modal(document.getElementById('purchaseHistoryModal'));
    modal.show();
}