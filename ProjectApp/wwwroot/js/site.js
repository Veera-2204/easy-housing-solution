/* Enhanced Site JavaScript for EasyHousing */

// Header scroll effect
document.addEventListener('DOMContentLoaded', function() {
    const header = document.querySelector('.app-header');
    const body = document.body;
    let lastScrollTop = 0;
    let ticking = false;

    function updateHeaderOnScroll() {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        
        // Add scrolled class when page is scrolled
        if (scrollTop > 20) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }

        lastScrollTop = scrollTop;
        ticking = false;
    }

    function requestTick() {
        if (!ticking) {
            requestAnimationFrame(updateHeaderOnScroll);
            ticking = true;
        }
    }

    // Throttled scroll listener
    window.addEventListener('scroll', requestTick, { passive: true });

    // Global Cursor Following Effect
    let mouseX = 0;
    let mouseY = 0;
    let isMoving = false;
    let moveTimeout;

    function updateCursorPosition(e) {
        mouseX = e.clientX;
        mouseY = e.clientY;
        
        // Update global cursor position
        body.style.setProperty('--cursor-x', mouseX + 'px');
        body.style.setProperty('--cursor-y', mouseY + 'px');
        body.style.setProperty('--show-cursor-blur', '1');
        
        // Clear previous timeout
        clearTimeout(moveTimeout);
        isMoving = true;
        
        // Hide blur after mouse stops moving
        moveTimeout = setTimeout(() => {
            isMoving = false;
            body.style.setProperty('--show-cursor-blur', '0');
        }, 1000);
    }

    // Add global mouse move listener
    document.addEventListener('mousemove', EasyHousing.debounce(updateCursorPosition, 16), { passive: true });

    // Enhanced cursor effect for specific containers
    function initEnhancedCursorEffect() {
        const enhancedElements = document.querySelectorAll('.property-card, .auth-wrapper, .hero-content, .card, .form, .dashboard-card, .enhanced-cursor-effect');
        
        enhancedElements.forEach(element => {
            element.addEventListener('mouseenter', function() {
                this.style.setProperty('--show-cursor-blur', '1');
            });

            element.addEventListener('mouseleave', function() {
                this.style.setProperty('--show-cursor-blur', '0');
            });

            element.addEventListener('mousemove', function(e) {
                const rect = this.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                
                this.style.setProperty('--cursor-x', x + 'px');
                this.style.setProperty('--cursor-y', y + 'px');
                this.style.setProperty('--show-cursor-blur', '1');
            });
        });
    }

    // Initialize enhanced cursor effects
    initEnhancedCursorEffect();

    // Re-initialize on dynamic content changes
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                // Check if any of the added nodes contain elements that need cursor effects
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1) { // Element node
                        const enhancedElements = node.querySelectorAll && node.querySelectorAll('.property-card, .auth-wrapper, .hero-content, .card, .form, .dashboard-card, .enhanced-cursor-effect');
                        if (enhancedElements && enhancedElements.length > 0) {
                            initEnhancedCursorEffect();
                        }
                    }
                });
            }
        });
    });

    // Start observing
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    // Enhanced dropdown menu handling with better contrast
    const dropdownToggle = document.querySelector('.user-menu');
    if (dropdownToggle) {
        dropdownToggle.addEventListener('click', function() {
            const isExpanded = this.getAttribute('aria-expanded') === 'true';
            this.setAttribute('aria-expanded', !isExpanded);
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', function(event) {
            if (!dropdownToggle.contains(event.target)) {
                dropdownToggle.setAttribute('aria-expanded', 'false');
                const dropdownMenu = document.querySelector('.dropdown-menu');
                if (dropdownMenu) {
                    dropdownMenu.classList.remove('show');
                }
            }
        });
    }

    // Enhanced mobile menu handling
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');
    
    if (navbarToggler && navbarCollapse) {
        navbarToggler.addEventListener('click', function() {
            const isExpanded = this.getAttribute('aria-expanded') === 'true';
            this.setAttribute('aria-expanded', !isExpanded);
            
            if (!isExpanded) {
                navbarCollapse.classList.add('show');
            } else {
                navbarCollapse.classList.remove('show');
            }
        });

        // Close mobile menu when clicking on a link
        const navLinks = navbarCollapse.querySelectorAll('.nav-link');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                navbarToggler.setAttribute('aria-expanded', 'false');
                navbarCollapse.classList.remove('show');
            });
        });
    }

    // Smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Page transition animations
    const pageContent = document.querySelector('.app-main');
    if (pageContent) {
        pageContent.classList.add('page-content');
    }

    // Loading state management for forms with enhanced dropdown handling
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function() {
            const submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
            submitButtons.forEach(button => {
                if (!button.disabled) {
                    button.disabled = true;
                    const originalText = button.textContent;
                    button.textContent = 'Processing...';
                    
                    // Re-enable after 5 seconds as fallback
                    setTimeout(() => {
                        button.disabled = false;
                        button.textContent = originalText;
                    }, 5000);
                }
            });
        });
    });

    // Enhanced tooltip functionality
    function initTooltips() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        initTooltips();
    }

    // Enhanced dropdown styling for better contrast
    function enhanceDropdownContrast() {
        const dropdowns = document.querySelectorAll('select, .dropdown-menu, .form-input');
        dropdowns.forEach(dropdown => {
            // Add high contrast styling
            dropdown.style.backgroundColor = '#ffffff';
            dropdown.style.color = '#212529';
            dropdown.style.border = '2px solid #495057';
            dropdown.style.borderRadius = '0.375rem';
            
            if (dropdown.tagName === 'SELECT') {
                dropdown.style.padding = '0.75rem 2.5rem 0.75rem 0.75rem';
                dropdown.style.fontSize = '1rem';
                dropdown.style.lineHeight = '1.5';
                
                // Enhanced focus state
                dropdown.addEventListener('focus', function() {
                    this.style.borderColor = '#0d6efd';
                    this.style.boxShadow = '0 0 0 0.2rem rgba(13, 110, 253, 0.25)';
                    this.style.outline = 'none';
                });
                
                dropdown.addEventListener('blur', function() {
                    this.style.borderColor = '#495057';
                    this.style.boxShadow = 'none';
                });
                
                // Enhanced hover state
                dropdown.addEventListener('mouseenter', function() {
                    if (!this.disabled) {
                        this.style.borderColor = '#6c757d';
                    }
                });
                
                dropdown.addEventListener('mouseleave', function() {
                    if (!this.matches(':focus')) {
                        this.style.borderColor = '#495057';
                    }
                });
            }
        });
        
        // Style dropdown options for better contrast
        const options = document.querySelectorAll('option');
        options.forEach(option => {
            option.style.backgroundColor = '#ffffff';
            option.style.color = '#212529';
            option.style.padding = '0.5rem';
        });
    }

    // Apply enhanced dropdown contrast on load
    enhanceDropdownContrast();

    // Lazy loading for images
    const images = document.querySelectorAll('img[data-src]');
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.classList.remove('lazy');
                imageObserver.unobserve(img);
            }
        });
    });

    images.forEach(img => {
        imageObserver.observe(img);
    });

    // Performance optimization: Preload critical resources
    function preloadResource(href, as) {
        const link = document.createElement('link');
        link.rel = 'preload';
        link.href = href;
        link.as = as;
        document.head.appendChild(link);
    }

    // Dark mode preference detection
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        // User prefers dark mode
        document.body.classList.add('dark-mode-preferred');
    }

    // Theme color meta tag for mobile browsers
    const themeColorMeta = document.createElement('meta');
    themeColorMeta.name = 'theme-color';
    themeColorMeta.content = '#181a20';
    document.head.appendChild(themeColorMeta);

    // Accessibility: Respect reduced motion preferences
    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        // Disable cursor effects for users who prefer reduced motion
        body.style.setProperty('--show-cursor-blur', '0');
        document.removeEventListener('mousemove', updateCursorPosition);
    }
});

// Utility functions
window.EasyHousing = {
    // Show notification (for future use)
    showNotification: function(message, type = 'info') {
        // Implementation for notification system
        console.log(`${type.toUpperCase()}: ${message}`);
    },

    // Scroll to top
    scrollToTop: function() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    },

    // Format currency
    formatCurrency: function(amount, currency = 'USD') {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: currency
        }).format(amount);
    },

    // Debounce function for performance
    debounce: function(func, wait, immediate) {
        let timeout;
        return function executedFunction() {
            const context = this;
            const args = arguments;
            const later = function() {
                timeout = null;
                if (!immediate) func.apply(context, args);
            };
            const callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func.apply(context, args);
        };
    },

    // Initialize cursor effects manually if needed
    initCursorEffects: function() {
        const enhancedElements = document.querySelectorAll('.property-card, .auth-wrapper, .hero-content, .card, .form, .dashboard-card, .enhanced-cursor-effect');
        
        enhancedElements.forEach(element => {
            element.addEventListener('mouseenter', function() {
                this.style.setProperty('--show-cursor-blur', '1');
            });

            element.addEventListener('mouseleave', function() {
                this.style.setProperty('--show-cursor-blur', '0');
            });

            element.addEventListener('mousemove', function(e) {
                const rect = this.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                
                this.style.setProperty('--cursor-x', x + 'px');
                this.style.setProperty('--cursor-y', y + 'px');
                this.style.setProperty('--show-cursor-blur', '1');
            });
        });
    },

    // Enhanced dropdown contrast utility
    enhanceDropdownContrast: function() {
        const dropdowns = document.querySelectorAll('select, .dropdown-menu, .form-input');
        dropdowns.forEach(dropdown => {
            dropdown.style.backgroundColor = '#ffffff';
            dropdown.style.color = '#212529';
            dropdown.style.border = '2px solid #495057';
            dropdown.style.borderRadius = '0.375rem';
            
            if (dropdown.tagName === 'SELECT') {
                dropdown.style.padding = '0.75rem 2.5rem 0.75rem 0.75rem';
                dropdown.style.fontSize = '1rem';
                dropdown.style.lineHeight = '1.5';
            }
        });
    }
};

// Export for module systems if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = window.EasyHousing;
}
