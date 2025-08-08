// Payment Process JavaScript

let currentStep = 1;
let selectedPackage = null;
let packagesData = @Html.Raw(Json.Serialize(ViewBag.Packages));

// Initialize
document.addEventListener('DOMContentLoaded', function () {
    updateUI();
});

function selectPackage(packageElement) {
    // Remove previous selection
    document.querySelectorAll('.package-card').forEach(card => {
        card.classList.remove('selected');
    });

    // Add selection to clicked package
    packageElement.classList.add('selected');

    // Get package data
    const packageId = packageElement.dataset.packageId;
    selectedPackage = packagesData.find(p => p.id === packageId);

    // Enable next button
    document.getElementById('nextBtn').disabled = false;

    console.log('Selected package:', selectedPackage);
}

function nextStep() {
    if (currentStep === 1) {
        if (!selectedPackage) {
            alert('Lütfen bir paket seçiniz.');
            return;
        }

        // Update selected package summary
        updatePackageSummary();
        currentStep = 2;
    } else if (currentStep === 2) {
        if (!validateCustomerForm()) {
            return;
        }

        // Start payment process
        startPaymentProcess();
        currentStep = 3;
    }

    updateUI();
}

function previousStep() {
    if (currentStep > 1) {
        currentStep--;
        updateUI();
    }
}

function updateUI() {
    // Hide all steps
    document.querySelectorAll('.payment-step').forEach(step => {
        step.classList.remove('active');
    });

    // Show current step
    document.getElementById(`step${currentStep}`).classList.add('active');

    // Update step indicators
    for (let i = 1; i <= 3; i++) {
        const indicator = document.getElementById(`step${i}-indicator`);
        indicator.classList.remove('active', 'completed');

        if (i < currentStep) {
            indicator.classList.add('completed');
        } else if (i === currentStep) {
            indicator.classList.add('active');
        }
    }

    // Update navigation buttons
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    if (currentStep === 1) {
        prevBtn.style.display = 'none';
        nextBtn.disabled = !selectedPackage;
        nextBtn.innerHTML = 'Devam Et <i class="fas fa-arrow-right ms-2"></i>';
    } else if (currentStep === 2) {
        prevBtn.style.display = 'inline-block';
        nextBtn.disabled = false;
        nextBtn.innerHTML = 'Ödemeye Geç <i class="fas fa-credit-card ms-2"></i>';
    } else if (currentStep === 3) {
        prevBtn.style.display = 'inline-block';
        nextBtn.style.display = 'none';
    }
}

function updatePackageSummary() {
    if (!selectedPackage) return;

    document.getElementById('selectedPackageId').value = selectedPackage.id;
    document.getElementById('selectedPackageName').textContent = selectedPackage.caption;

    const duration = selectedPackage.dayCount
        ? `${selectedPackage.monthCount} ay + ${selectedPackage.dayCount} gün`
        : `${selectedPackage.monthCount} ay`;

    document.getElementById('selectedPackageDuration').textContent = duration;
    document.getElementById('selectedPackageDescription').textContent = selectedPackage.description;
    document.getElementById('selectedPackagePrice').textContent = selectedPackage.price.toFixed(2) + ' ₺';
}

function validateCustomerForm() {
    const form = document.getElementById('customerForm');

    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return false;
    }

    return true;
}

function startPaymentProcess() {
    const formData = new FormData(document.getElementById('customerForm'));

    // Show loading
    document.querySelector('.payment-loading').style.display = 'block';
    document.getElementById('paymentIframe').style.display = 'none';

    // Send request to start payment
    fetch('/payment/process', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success && data.token) {
                // Load PayTR iframe
                loadPaymentIframe(data.token);
            } else {
                alert('Ödeme başlatılamadı: ' + (data.message || 'Bilinmeyen hata'));
                previousStep();
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Bir hata oluştu. Lütfen tekrar deneyin.');
            previousStep();
        });
}

function loadPaymentIframe(token) {
    const iframe = document.getElementById('paymentIframe');
    iframe.src = `https://www.paytr.com/odeme/guvenli/${token}`;

    // Hide loading and show iframe
    document.querySelector('.payment-loading').style.display = 'none';
    iframe.style.display = 'block';

    // Initialize iframe resizer
    iFrameResize({}, '#paymentIframe');
}