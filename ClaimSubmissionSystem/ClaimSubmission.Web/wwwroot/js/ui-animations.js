/**
 * UI Animations & Interactions
 * Smooth transitions and professional animations
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize smooth scrolling and animations
    initializeAnimations();
    initializeFormValidation();
    initializeTooltips();
    initializeScrollEffects();
});

/**
 * Initialize page animations
 */
function initializeAnimations() {
    // Add fade-in animation to elements with data-animate attribute
    const animatedElements = document.querySelectorAll('[data-animate]');
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-fade-in');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    animatedElements.forEach(el => observer.observe(el));

    // Animate cards on load
    const cards = document.querySelectorAll('.card');
    cards.forEach((card, index) => {
        setTimeout(() => {
            card.style.animation = 'slideIn 0.3s ease-out forwards';
        }, index * 50);
    });
}

/**
 * Form validation animations
 */
function initializeFormValidation() {
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        const inputs = form.querySelectorAll('.form-control, .form-select');
        
        inputs.forEach(input => {
            input.addEventListener('invalid', function(e) {
                e.preventDefault();
                this.classList.add('is-invalid');
                this.style.animation = 'pulse 0.5s ease-in-out';
            });

            input.addEventListener('input', function() {
                if (this.classList.contains('is-invalid')) {
                    if (this.checkValidity()) {
                        this.classList.remove('is-invalid');
                        this.classList.add('is-valid');
                    }
                }
            });
        });
    });
}

/**
 * Initialize tooltips and popovers
 */
function initializeTooltips() {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
        new bootstrap.Tooltip(el);
    });
}

/**
 * Scroll effects for header
 */
function initializeScrollEffects() {
    let lastScrollTop = 0;
    const navbar = document.querySelector('.navbar');
    
    if (!navbar) return;

    window.addEventListener('scroll', function() {
        let scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        
        if (scrollTop > 50) {
            navbar.style.boxShadow = '0 4px 12px rgba(0, 0, 0, 0.15)';
        } else {
            navbar.style.boxShadow = '';
        }
        
        lastScrollTop = scrollTop;
    });
}

/**
 * Show success notification
 */
function showSuccessNotification(message) {
    showNotification(message, 'success');
}

/**
 * Show error notification
 */
function showErrorNotification(message) {
    showNotification(message, 'danger');
}

/**
 * Show warning notification
 */
function showWarningNotification(message) {
    showNotification(message, 'warning');
}

/**
 * Generic notification display
 */
function showNotification(message, type = 'info') {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show animate-slide-in`;
    alertDiv.setAttribute('role', 'alert');
    
    const iconMap = {
        'success': '✅',
        'danger': '⚠️',
        'warning': '⚡',
        'info': 'ℹ️'
    };
    
    alertDiv.innerHTML = `
        <div class="d-flex align-items-start gap-3">
            <span>${iconMap[type] || ''}</span>
            <div style="flex: 1;">${message}</div>
            <button type="button" class="btn-close alert-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;
    
    // Insert at top of main content
    const main = document.querySelector('main');
    if (main) {
        main.insertBefore(alertDiv, main.firstChild);
    } else {
        document.body.insertBefore(alertDiv, document.body.firstChild);
    }
    
    // Auto-dismiss after 6 seconds
    setTimeout(() => {
        const alert = bootstrap.Alert.getOrCreateInstance(alertDiv);
        alert.close();
    }, 6000);
}

/**
 * Smooth button loading state
 */
function setButtonLoading(buttonElement, isLoading = true) {
    if (isLoading) {
        buttonElement.disabled = true;
        buttonElement.dataset.originalText = buttonElement.innerHTML;
        buttonElement.innerHTML = '<span class="spinner spinner-sm me-2"></span>Loading...';
    } else {
        buttonElement.disabled = false;
        buttonElement.innerHTML = buttonElement.dataset.originalText;
    }
}

/**
 * Table row highlight animation
 */
function highlightTableRow(rowElement) {
    rowElement.style.backgroundColor = 'rgba(59, 130, 246, 0.1)';
    setTimeout(() => {
        rowElement.style.transition = 'background-color 0.3s ease';
        rowElement.style.backgroundColor = '';
    }, 100);
}

/**
 * Smooth page transition
 */
function smoothTransition(url) {
    document.body.style.opacity = '0.5';
    window.location.href = url;
    setTimeout(() => {
        document.body.style.opacity = '1';
    }, 300);
}

/**
 * Initialize modal animations
 */
document.addEventListener('show.bs.modal', function(e) {
    const modal = e.target;
    modal.style.animation = 'slideIn 0.3s ease-out';
});

document.addEventListener('hide.bs.modal', function(e) {
    const modal = e.target;
    modal.style.animation = 'fadeIn 0.2s ease-in reverse';
});

// Export functions for use in views
window.UIAnimations = {
    showSuccess: showSuccessNotification,
    showError: showErrorNotification,
    showWarning: showWarningNotification,
    notify: showNotification,
    setButtonLoading,
    highlightTableRow,
    smoothTransition
};
