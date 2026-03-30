/**
 * Global API Interceptor
 * Automatically adds authentication token to all API requests
 * and handles 401 responses by redirecting to login
 */

/**
 * Override the native fetch function to intercept all API calls
 */
const originalFetch = window.fetch;

window.fetch = function(...args) {
    const [resource, config] = args;
    const url = typeof resource === 'string' ? resource : resource.url;

    // Only intercept API calls (not static assets)
    if (url && (url.includes('/api/') || url.includes('/claim/') || url.includes('/authentication/'))) {
        const token = authStorage.getToken();
        const headers = config?.headers || {};

        // Add authorization header if token exists
        if (token && !headers['Authorization']) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        // Ensure content type is set for JSON
        if (!headers['Content-Type'] && config?.body) {
            headers['Content-Type'] = 'application/json';
        }

        const modifiedConfig = {
            ...config,
            headers: headers
        };

        // Call the original fetch with modified config
        return originalFetch(resource, modifiedConfig)
            .then(response => {
                // Handle 401 Unauthorized
                if (response.status === 401) {
                    console.warn('[Auth Interceptor] Received 401 Unauthorized - redirecting to login');
                    authStorage.clearAuth();
                    
                    // Show a brief notification before redirecting
                    const notificationDiv = document.createElement('div');
                    notificationDiv.style.cssText = `
                        position: fixed;
                        top: 20px;
                        left: 50%;
                        transform: translateX(-50%);
                        background-color: #ef4444;
                        color: white;
                        padding: 16px 24px;
                        border-radius: 8px;
                        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
                        z-index: 9999;
                        font-weight: 500;
                    `;
                    notificationDiv.textContent = 'Session expired. Please log in again.';
                    document.body.appendChild(notificationDiv);

                    setTimeout(() => {
                        window.location.href = '/Authentication/Login';
                    }, 2000);
                }

                // Log API responses for debugging
                if (!response.ok && response.status >= 400) {
                    console.warn(`[API] ${response.status} ${response.statusText} - ${url}`);
                }

                return response;
            })
            .catch(error => {
                console.error(`[API] Request failed: ${url}`, error);
                throw error;
            });
    }

    // For non-API calls, just call the original fetch
    return originalFetch.apply(this, args);
};

/**
 * XMLHttpRequest interceptor for older code that might use XHR
 */
const originalOpen = XMLHttpRequest.prototype.open;

XMLHttpRequest.prototype.open = function(method, url, ...rest) {
    // Store the URL for later use in setRequestHeader
    this._requestUrl = url;
    return originalOpen.call(this, method, url, ...rest);
};

const originalSetRequestHeader = XMLHttpRequest.prototype.setRequestHeader;

XMLHttpRequest.prototype.setRequestHeader = function(header, value) {
    // Add authorization header if making API call
    if (this._requestUrl && (this._requestUrl.includes('/api/') || this._requestUrl.includes('/claim/'))) {
        if (header.toLowerCase() === 'authorization' || !this._authHeaderSet) {
            const token = authStorage.getToken();
            if (token && header.toLowerCase() !== 'authorization') {
                this.setRequestHeader('Authorization', `Bearer ${token}`);
                this._authHeaderSet = true;
            }
        }
    }

    return originalSetRequestHeader.call(this, header, value);
};

/**
 * Handle XHR responses
 */
const originalOnReadyStateChange = XMLHttpRequest.prototype.onreadystatechange;

Object.defineProperty(XMLHttpRequest.prototype, 'onreadystatechange', {
    get: function() {
        return this._onreadystatechange;
    },
    set: function(callback) {
        this._onreadystatechange = function() {
            // Check for 401 response
            if (this.readyState === 4 && this.status === 401) {
                console.warn('[XHR Interceptor] Received 401 Unauthorized');
                authStorage.clearAuth();
                window.location.href = '/Authentication/Login';
            }
            return callback?.call(this);
        };
    }
});

console.log('[Auth Interceptor] Global API interceptor initialized');
