/**
 * Security helpers for CSRF protection and XSS prevention
 */

/**
 * Gets the anti-forgery token value from the page
 * @returns {string} The CSRF token value
 */
function getAntiForgeryToken() {
    // Look for the token in the form or hidden input
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (tokenInput) {
        return tokenInput.value;
    }
    
    // Alternative: check in meta tag if using a custom setup
    const tokenMeta = document.querySelector('meta[name="csrf-token"]');
    if (tokenMeta) {
        return tokenMeta.getAttribute('content');
    }
    
    return '';
}

/**
 * Safely sets text content (prevents XSS)
 * @param {Element} element - The DOM element
 * @param {string} text - The text to set
 */
function setSafeText(element, text) {
    if (element) {
        element.textContent = text;
    }
}

/**
 * Creates a safe HTML element without using innerHTML
 * @param {string} tagName - HTML tag name
 * @param {Object} options - { className, id, textContent, innerHTML, attributes }
 * @returns {Element}
 */
function createSafeElement(tagName, options = {}) {
    const element = document.createElement(tagName);
    
    if (options.className) {
        element.className = options.className;
    }
    
    if (options.id) {
        element.id = options.id;
    }
    
    if (options.textContent) {
        element.textContent = options.textContent;
    }
    
    if (options.attributes) {
        for (const [key, value] of Object.entries(options.attributes)) {
            element.setAttribute(key, value);
        }
    }
    
    return element;
}

/**
 * Enhanced fetch with automatic CSRF token injection
 * @param {string} url - The URL to fetch
 * @param {Object} options - Fetch options
 * @returns {Promise<Response>}
 */
async function securePost(url, options = {}) {
    const defaultOptions = {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': getAntiForgeryToken()
        }
    };
    
    // Merge options, with special handling for headers
    const mergedOptions = {
        ...defaultOptions,
        ...options,
        headers: {
            ...defaultOptions.headers,
            ...options.headers
        }
    };
    
    return fetch(url, mergedOptions);
}

/**
 * Sanitizes HTML to prevent XSS attacks (basic version)
 * For production, consider using a library like DOMPurify
 * @param {string} html - The HTML string to sanitize
 * @returns {string} - Sanitized HTML
 */
function sanitizeHTML(html) {
    const div = document.createElement('div');
    div.textContent = html;
    return div.innerHTML;
}

/**
 * Safely displays error messages
 * @param {Element} element - The container element
 * @param {string} message - The error message
 */
function displayError(element, message) {
    if (element) {
        element.textContent = message; // Use textContent to prevent XSS
        element.style.display = 'block';
    }
}

/**
 * Clears error display
 * @param {Element} element - The container element
 */
function clearError(element) {
    if (element) {
        element.textContent = '';
        element.style.display = 'none';
    }
}
