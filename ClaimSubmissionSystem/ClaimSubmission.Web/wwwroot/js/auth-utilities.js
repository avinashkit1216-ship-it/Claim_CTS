/**
 * Authentication Utilities - Local Storage Management
 * Handles user data persistence and token management
 */

// Local Storage Keys
const AUTH_STORAGE_KEYS = {
    USER: 'user',
    TOKEN: 'token',
    USER_ID: 'userId',
    USERNAME: 'username',
    EMAIL: 'email',
    FULL_NAME: 'fullName',
    IS_AUTHENTICATED: 'isAuthenticated',
    TOKEN_EXPIRY: 'tokenExpiry'
};

/**
 * Save user to local storage
 * @param {Object} userData - User object with userId, username, email, fullName
 * @param {string} token - JWT token
 */
function saveUserToLocalStorage(userData, token) {
    try {
        if (!userData || !token) {
            console.error('Invalid user data or token');
            return false;
        }

        // Save user details
        localStorage.setItem(AUTH_STORAGE_KEYS.USER, JSON.stringify(userData));
        localStorage.setItem(AUTH_STORAGE_KEYS.TOKEN, token);
        localStorage.setItem(AUTH_STORAGE_KEYS.USER_ID, userData.userId || '');
        localStorage.setItem(AUTH_STORAGE_KEYS.USERNAME, userData.username || '');
        localStorage.setItem(AUTH_STORAGE_KEYS.EMAIL, userData.email || '');
        localStorage.setItem(AUTH_STORAGE_KEYS.FULL_NAME, userData.fullName || '');
        localStorage.setItem(AUTH_STORAGE_KEYS.IS_AUTHENTICATED, 'true');

        // Set token expiry (default: 24 hours from now)
        const expiryTime = new Date().getTime() + (24 * 60 * 60 * 1000);
        localStorage.setItem(AUTH_STORAGE_KEYS.TOKEN_EXPIRY, expiryTime.toString());

        console.log('User data saved to localStorage for:', userData.email);
        return true;
    } catch (error) {
        console.error('Error saving user to localStorage:', error);
        return false;
    }
}

/**
 * Retrieve user from local storage
 * @returns {Object|null} User object or null if not found
 */
function getUserFromLocalStorage() {
    try {
        const userJson = localStorage.getItem(AUTH_STORAGE_KEYS.USER);
        return userJson ? JSON.parse(userJson) : null;
    } catch (error) {
        console.error('Error retrieving user from localStorage:', error);
        return null;
    }
}

/**
 * Get authentication token from local storage
 * @returns {string|null} Token or null if not found
 */
function getTokenFromLocalStorage() {
    try {
        return localStorage.getItem(AUTH_STORAGE_KEYS.TOKEN) || null;
    } catch (error) {
        console.error('Error retrieving token from localStorage:', error);
        return null;
    }
}

/**
 * Check if user is authenticated
 * @returns {boolean} True if user is authenticated
 */
function isUserAuthenticated() {
    try {
        // Check if token exists and is not expired
        const token = getTokenFromLocalStorage();
        const expiryTime = localStorage.getItem(AUTH_STORAGE_KEYS.TOKEN_EXPIRY);

        if (!token) {
            return false;
        }

        if (expiryTime && new Date().getTime() > parseInt(expiryTime)) {
            console.warn('Token expired, clearing localStorage');
            clearUserFromLocalStorage();
            return false;
        }

        return localStorage.getItem(AUTH_STORAGE_KEYS.IS_AUTHENTICATED) === 'true';
    } catch (error) {
        console.error('Error checking authentication status:', error);
        return false;
    }
}

/**
 * Clear user data from local storage (logout)
 */
function clearUserFromLocalStorage() {
    try {
        Object.values(AUTH_STORAGE_KEYS).forEach(key => {
            localStorage.removeItem(key);
        });
        console.log('User data cleared from localStorage');
    } catch (error) {
        console.error('Error clearing localStorage:', error);
    }
}

/**
 * Refresh token expiry time
 */
function refreshTokenExpiry() {
    try {
        const expiryTime = new Date().getTime() + (24 * 60 * 60 * 1000);
        localStorage.setItem(AUTH_STORAGE_KEYS.TOKEN_EXPIRY, expiryTime.toString());
    } catch (error) {
        console.error('Error refreshing token expiry:', error);
    }
}

/**
 * Get authorization header for API requests
 * @returns {string|null} Bearer token string or null if not authenticated
 */
function getAuthorizationHeader() {
    const token = getTokenFromLocalStorage();
    return token ? `Bearer ${token}` : null;
}

/**
 * Perform automatic login after registration
 * @param {string} apiBaseUrl - Base URL for API
 * @param {string} username - Username for login
 * @param {string} password - Password for login
 * @returns {Promise<boolean>} True if login successful
 */
async function autoLoginAfterRegistration(apiBaseUrl, username, password) {
    try {
        console.log('Attempting auto-login for:', username);

        const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username: username,
                password: password
            })
        });

        if (!response.ok) {
            console.error('Auto-login failed with status:', response.status);
            return false;
        }

        const data = await response.json();
        
        if (data.data) {
            const userData = {
                userId: data.data.userId,
                username: data.data.username,
                email: data.data.email,
                fullName: data.data.fullName
            };

            const token = data.data.token;
            saveUserToLocalStorage(userData, token);
            return true;
        }

        return false;
    } catch (error) {
        console.error('Error during auto-login:', error);
        return false;
    }
}

/**
 * Add authorization header to fetch options
 * @param {Object} options - Fetch options object
 * @returns {Object} Updated options with authorization header
 */
function addAuthorizationToFetchOptions(options = {}) {
    const authHeader = getAuthorizationHeader();
    if (authHeader) {
        options.headers = {
            ...options.headers,
            'Authorization': authHeader
        };
    }
    return options;
}

/**
 * Redirect to home if not authenticated
 * @param {string} loginUrl - URL to redirect to if not authenticated
 */
function redirectIfNotAuthenticated(loginUrl = '/Authentication/Login') {
    if (!isUserAuthenticated()) {
        clearUserFromLocalStorage();
        window.location.href = loginUrl;
    }
}

/**
 * Redirect to dashboard if authenticated
 * @param {string} dashboardUrl - URL to redirect to if authenticated
 */
function redirectIfAuthenticated(dashboardUrl = '/Claim/Index') {
    if (isUserAuthenticated()) {
        window.location.href = dashboardUrl;
    }
}

/**
 * Display user info in UI
 * @param {string} elementId - ID of element to display user info in
 */
function displayUserInfoInUI(elementId = 'userDisplay') {
    try {
        const user = getUserFromLocalStorage();
        const element = document.getElementById(elementId);

        if (element && user) {
            element.textContent = `Welcome, ${user.fullName || user.username}`;
        }
    } catch (error) {
        console.error('Error displaying user info:', error);
    }
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        AUTH_STORAGE_KEYS,
        saveUserToLocalStorage,
        getUserFromLocalStorage,
        getTokenFromLocalStorage,
        isUserAuthenticated,
        clearUserFromLocalStorage,
        refreshTokenExpiry,
        getAuthorizationHeader,
        autoLoginAfterRegistration,
        addAuthorizationToFetchOptions,
        redirectIfNotAuthenticated,
        redirectIfAuthenticated,
        displayUserInfoInUI
    };
}
