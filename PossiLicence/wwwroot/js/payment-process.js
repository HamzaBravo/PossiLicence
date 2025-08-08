
// Payment Process JavaScript
let currentStep = 1;
let selectedPackage = null;
let packagesData = [];

// DOM yüklendiğinde çalışacak fonksiyonlar
function initializePaymentProcess() {
    console.log('Initializing payment process...');
    
    loadPackagesData();
    setupEventListeners();
    updateUI();
    
    console.log('Payment process initialized');
}

function loadPackagesData() {
    console.log('Loading packages data...');
    const packageCards = document.querySelectorAll('.package-card');
    console.log('Found package cards:', packageCards.length);

    packagesData = Array.from(packageCards).map(card => {
        const priceElement = card.querySelector('.package-price');
        const featureElement = card.querySelector('.feature');
        const h5Element = card.querySelector('h5');
        const descElement = card.querySelector('.feature:last-child span');

        if (!priceElement || !featureElement || !h5Element) {
            console.error('Missing elements in package card:', card);
            return null;
        }

        const priceText = priceElement.textContent;
        const featureText = featureElement.textContent;

        return {
            id: card.dataset.packageId,
            caption: h5Element.textContent,
            price: parseFloat(priceText.replace(/[^\d.,]/g, '').replace(',', '.')),
            monthCount: extractMonthCount(featureText),
            dayCount: extractDayCount(featureText),
            description: descElement ? descElement.textContent : featureText
        };
    }).filter(p => p !== null);

    console.log('Loaded packages:', packagesData);
}

function setupEventListeners() {
    console.log('Setting up event listeners...');
    
    // Package selection event listeners
    document.querySelectorAll('.package-card').forEach(card => {
        card.addEventListener('click', function(e) {
            e.preventDefault();
            selectPackage(this);
        });
    });
    
    // Button event listeners
    const nextBtn = document.getElementById('nextBtn');
    const prevBtn = document.getElementById('prevBtn');
    
    if (nextBtn) {
        nextBtn.addEventListener('click', function(e) {
            e.preventDefault();
            nextStep();
        });
    }
    
    if (prevBtn) {
        prevBtn.addEventListener('click', function(e) {
            e.preventDefault();
            previousStep();
        });
    }
    
    // Form validation event listeners
    const form = document.getElementById('customerForm');
    if (form) {
        const inputs = form.querySelectorAll('input[required], textarea[required]');
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                validateField(this);
            });
            
            input.addEventListener('input', function() {
                if (this.classList.contains('is-invalid') && this.checkValidity()) {
                    this.classList.remove('is-invalid');
                    this.classList.add('is-valid');
                }
            });
        });
    }
    
    console.log('Event listeners set up');
}

function validateField(field) {
    if (field.checkValidity()) {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
    } else {
        field.classList.remove('is-valid');
        field.classList.add('is-invalid');
    }
}

function extractMonthCount(text) {
    const match = text.match(/(\d+)\s*ay/);
    return match ? parseInt(match[1]) : 0;
}

function extractDayCount(text) {
    const match = text.match(/\+\s*(\d+)\s*gün/);
    return match ? parseInt(match[1]) : null;
}

function selectPackage(packageElement) {
    console.log('Package selected:', packageElement);

    // Remove previous selection
    document.querySelectorAll('.package-card').forEach(card => {
        card.classList.remove('selected');
    });

    // Add selection to clicked package
    packageElement.classList.add('selected');

    // Get package data
    const packageId = packageElement.dataset.packageId;
    selectedPackage = packagesData.find(p => p.id === packageId);

    console.log('Selected package data:', selectedPackage);

    // Enable next button
    const nextBtn = document.getElementById('nextBtn');
    if (nextBtn) {
        nextBtn.disabled = false;
        console.log('Next button enabled');
    }
}

function nextStep() {
    console.log('Next step called, current step:', currentStep);

    if (currentStep === 1) {
        if (!selectedPackage) {
            showAlert('Lütfen bir paket seçiniz.', 'warning');
            return;
        }

        updatePackageSummary();
        currentStep = 2;
    } else if (currentStep === 2) {
        if (!validateCustomerForm()) {
            return;
        }

        startPaymentProcess();
        currentStep = 3;
    }

    updateUI();
}

function previousStep() {
    console.log('Previous step called, current step:', currentStep);

    if (currentStep > 1) {
        currentStep--;
        updateUI();

        // Reset iframe if going back from payment step
        if (currentStep === 2) {
            const iframe = document.getElementById('paymentIframe');
            if (iframe) {
                iframe.src = 'about:blank';
                iframe.style.display = 'none';
            }
            const loading = document.querySelector('.payment-loading');
            if (loading) {
                loading.style.display = 'none';
            }
        }
    }
}

function updateUI() {
    console.log('Updating UI for step:', currentStep);

    // Hide all steps
    document.querySelectorAll('.payment-step').forEach(step => {
        step.classList.remove('active');
    });

    // Show current step
    const currentStepElement = document.getElementById(`step${currentStep}`);
    if (currentStepElement) {
        currentStepElement.classList.add('active');
    }

    // Update step indicators
    for (let i = 1; i <= 3; i++) {
        const indicator = document.getElementById(`step${i}-indicator`);
        if (indicator) {
            indicator.classList.remove('active', 'completed');

            if (i < currentStep) {
                indicator.classList.add('completed');
            } else if (i === currentStep) {
                indicator.classList.add('active');
            }
        }
    }

    // Update navigation buttons
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    if (prevBtn && nextBtn) {
        if (currentStep === 1) {
            prevBtn.style.display = 'none';
            nextBtn.disabled = !selectedPackage;
            nextBtn.innerHTML = 'Devam Et <i class="fas fa-arrow-right ms-2"></i>';
            nextBtn.style.display = 'inline-block';
        } else if (currentStep === 2) {
            prevBtn.style.display = 'inline-block';
            nextBtn.disabled = false;
            nextBtn.innerHTML = 'Ödemeye Geç <i class="fas fa-credit-card ms-2"></i>';
            nextBtn.style.display = 'inline-block';
        } else if (currentStep === 3) {
            prevBtn.style.display = 'inline-block';
            nextBtn.style.display = 'none';
        }
    }
}

function updatePackageSummary() {
    if (!selectedPackage) return;

    const selectedPackageIdInput = document.getElementById('selectedPackageId');
    if (selectedPackageIdInput) {
        selectedPackageIdInput.value = selectedPackage.id;
    }

    const elements = {
        'selectedPackageName': selectedPackage.caption,
        'selectedPackageDuration': selectedPackage.dayCount
            ? `${selectedPackage.monthCount} ay + ${selectedPackage.dayCount} gün`
            : `${selectedPackage.monthCount} ay`,
        'selectedPackageDescription': selectedPackage.description,
        'selectedPackagePrice': selectedPackage.price.toFixed(2) + ' ₺'
    };

    Object.entries(elements).forEach(([id, value]) => {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    });
}

function validateCustomerForm() {
    const form = document.getElementById('customerForm');

    if (!form) {
        return false;
    }

    // Check HTML5 validation
    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        showAlert('Lütfen tüm gerekli alanları doldurun.', 'warning');
        return false;
    }

    // Additional custom validations
    const email = document.getElementById('email');
    if (email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!emailRegex.test(email.value)) {
            showAlert('Lütfen geçerli bir e-posta adresi girin.', 'warning');
            return false;
        }
    }

    return true;
}

function startPaymentProcess() {
    const form = document.getElementById('customerForm');
    const formData = new FormData(form);

    // Show loading
    const loadingElement = document.querySelector('.payment-loading');
    const iframeContainer = document.querySelector('.payment-iframe-container');

    if (loadingElement) loadingElement.style.display = 'block';
    if (iframeContainer) {
        // Clear existing iframe
        const existingIframe = iframeContainer.querySelector('#paymentIframe');
        if (existingIframe) {
            existingIframe.remove();
        }
    }

    // Send request to start payment
    fetch('/payment/process', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        if (data.success && data.token) {
            setTimeout(() => {
                loadPaymentIframe(data.token);
            }, 500);
        } else {
            const message = data.message || 'Ödeme başlatılamadı.';
            showAlert(message, 'error');
            previousStep();
        }
    })
    .catch(error => {
        console.error('Payment process error:', error);
        showAlert('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        previousStep();
    })
    .finally(() => {
        setTimeout(() => {
            if (loadingElement) loadingElement.style.display = 'none';
        }, 1000);
    });
}

function loadPaymentIframe(token) {
    const container = document.querySelector('.payment-iframe-container');
    
    if (container) {
        // Create new iframe
        const newIframe = document.createElement('iframe');
        newIframe.id = 'paymentIframe';
        newIframe.style.cssText = 'width: 100%; height: 600px; border-radius: 10px; border: none; display: block;';
        newIframe.frameBorder = '0';
        newIframe.scrolling = 'auto';

        // Iframe load event
        newIframe.onload = function() {
            console.log('PayTR iframe loaded successfully');
        };

        newIframe.onerror = function() {
            console.error('PayTR iframe load error');
            showAlert('Ödeme sayfası yüklenirken hata oluştu.', 'error');
        };

        // Add to container
        container.appendChild(newIframe);

        // Set source
        newIframe.src = `https://www.paytr.com/odeme/guvenli/${token}`;
    }
}

function showAlert(message, type = 'info') {
    const alertClass = type === 'error' ? 'alert-danger' :
        type === 'warning' ? 'alert-warning' :
        type === 'success' ? 'alert-success' : 'alert-info';

    const alert = document.createElement('div');
    alert.className = `alert ${alertClass} alert-dismissible fade show position-fixed`;
    alert.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(alert);

    setTimeout(() => {
        if (alert.parentNode) {
            alert.remove();
        }
    }, 5000);
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializePaymentProcess);
} else {
    // DOM already loaded
    initializePaymentProcess();
}

// Backup initialization for safety
window.addEventListener('load', function() {
    // Double check if not initialized
    if (packagesData.length === 0) {
        console.log('Backup initialization triggered');
        initializePaymentProcess();
    }
});