/**
 * Authentication Storage Manager
 * Handles localStorage operations for user data and authentication tokens
 */
class AuthStorageManager {
    constructor() {
        this.USER_KEY = 'user';
        this.TOKEN_KEY = 'token';
        this.USER_ID_KEY = 'userId';
        this.USERNAME_KEY = 'username';
        this.EMAIL_KEY = 'email';
        this.FULLNAME_KEY = 'fullName';
        this.AUTH_STATUS_KEY = 'isAuthenticated';
    }

    /**
     * Store user data in localStorage
     * @param {Object} userData - User data object { userId, username, email, fullName, token }
     */
    storeUser(userData) {
        try {
            if (!userData || !userData.email) {
                console.error('Invalid user data');
                return false;
            }

            // Store the complete user object
            localStorage.setItem(this.USER_KEY, JSON.stringify({
                userId: userData.userId,
                username: userData.username,
                emailAddress: userData.email,
                fullName: userData.fullName,
                registeredAt: new Date().toISOString()
            }));

            // Store individual fields for easy access
            localStorage.setItem(this.USER_ID_KEY, userData.userId || '');
            localStorage.setItem(this.USERNAME_KEY, userData.username || '');
            localStorage.setItem(this.EMAIL_KEY, userData.email || '');
            localStorage.setItem(this.FULLNAME_KEY, userData.fullName || '');

            // Store authentication status
            localStorage.setItem(this.AUTH_STATUS_KEY, 'true');

            console.log('User data stored in localStorage:', userData.username);
            return true;
        } catch (error) {
            console.error('Error storing user data:', error);
            return false;
        }
    }

    /**
     * Store authentication token in localStorage
     * @param {string} token - JWT token from the API
     */
    storeToken(token) {
        try {
            if (!token || typeof token !== 'string') {
                console.error('Invalid token');
                return false;
            }

            localStorage.setItem(this.TOKEN_KEY, token);
            console.log('Token stored in localStorage');
            return true;
        } catch (error) {
            console.error('Error storing token:', error);
            return false;
        }
    }

    /**
     * Retrieve user data from localStorage
     * @returns {Object|null} User data object or null
     */
    getUser() {
        try {
            const userJson = localStorage.getItem(this.USER_KEY);
            return userJson ? JSON.parse(userJson) : null;
        } catch (error) {
            console.error('Error retrieving user data:', error);
            return null;
        }
    }

    /**
     * Retrieve authentication token from localStorage
     * @returns {string|null} Token or null
     */
    getToken() {
        try {
            return localStorage.getItem(this.TOKEN_KEY);
        } catch (error) {
            console.error('Error retrieving token:', error);
            return null;
        }
    }

    /**
     * Retrieve specific user field from localStorage
     * @param {string} key - The key to retrieve (userId, username, email, fullName)
     * @returns {string|null} The value or null
     */
    getUserField(key) {
        try {
            const validKeys = [this.USER_ID_KEY, this.USERNAME_KEY, this.EMAIL_KEY, this.FULLNAME_KEY];
            if (!validKeys.includes(key)) {
                console.warn(`Invalid key: ${key}`);
                return null;
            }
            return localStorage.getItem(key);
        } catch (error) {
            console.error(`Error retrieving ${key}:`, error);
            return null;
        }
    }

    /**
     * Check if user is authenticated
     * @returns {boolean} True if user is authenticated
     */
    isAuthenticated() {
        const token = this.getToken();
        const user = this.getUser();
        const authStatus = localStorage.getItem(this.AUTH_STATUS_KEY) === 'true';
        return !!(token && user && authStatus);
    }

    /**
     * Clear all authentication data from localStorage
     */
    clearAuth() {
        try {
            localStorage.removeItem(this.USER_KEY);
            localStorage.removeItem(this.TOKEN_KEY);
            localStorage.removeItem(this.USER_ID_KEY);
            localStorage.removeItem(this.USERNAME_KEY);
            localStorage.removeItem(this.EMAIL_KEY);
            localStorage.removeItem(this.FULLNAME_KEY);
            localStorage.removeItem(this.AUTH_STATUS_KEY);
            console.log('Authentication data cleared from localStorage');
        } catch (error) {
            console.error('Error clearing authentication data:', error);
        }
    }

    /**
     * Get authentication headers for API requests
     * @returns {Object} Object with Authorization header if token exists
     */
    getAuthHeaders() {
        const token = this.getToken();
        if (token) {
            return {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            };
        }
        return {
            'Content-Type': 'application/json'
        };
    }

    /**
     * Store complete user session after registration/login
     * @param {Object} authResponse - Response from API containing user data and token
     */
    storeSession(authResponse) {
        try {
            if (!authResponse) {
                console.error('Invalid auth response');
                return false;
            }

            const userData = {
                userId: authResponse.userId || authResponse.data?.userId,
                username: authResponse.username || authResponse.data?.username,
                email: authResponse.email || authResponse.data?.email,
                fullName: authResponse.fullName || authResponse.data?.fullName,
                token: authResponse.token || authResponse.data?.token
            };

            const token = userData.token;

            // Store user data and token
            this.storeUser(userData);
            if (token) {
                this.storeToken(token);
            }

            console.log('Complete session stored');
            return true;
        } catch (error) {
            console.error('Error storing session:', error);
            return false;
        }
    }

    /**
     * Debug function to log current localStorage state
     */
    debugLog() {
        console.group('AuthStorage Debug Info');
        console.log('User:', this.getUser());
        console.log('Token:', this.getToken() ? this.getToken().substring(0, 20) + '...' : 'No token');
        console.log('IsAuthenticated:', this.isAuthenticated());
        console.log('Username:', this.getUserField(this.USERNAME_KEY));
        console.log('Email:', this.getUserField(this.EMAIL_KEY));
        console.groupEnd();
    }
}

// Create a global instance
const authStorage = new AuthStorageManager();

/**
 * API Helper with automatic token injection
 */
class ApiHelper {
    static async fetch(url, options = {}) {
        try {
            const headers = authStorage.getAuthHeaders();
            const finalOptions = {
                ...options,
                headers: {
                    ...headers,
                    ...options.headers
                }
            };

            const response = await fetch(url, finalOptions);

            // Handle 401 Unauthorized - token might be expired
            if (response.status === 401) {
                console.warn('Unauthorized - clearing session and redirecting to login');
                authStorage.clearAuth();
                window.location.href = '/Authentication/Login';
                return null;
            }

            return response;
        } catch (error) {
            console.error('API request error:', error);
            throw error;
        }
    }

    static async get(url) {
        const response = await this.fetch(url, {
            method: 'GET'
        });
        return response?.json();
    }

    static async post(url, data) {
        const response = await this.fetch(url, {
            method: 'POST',
            body: JSON.stringify(data)
        });
        return response?.json();
    }

    static async put(url, data) {
        const response = await this.fetch(url, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
        return response?.json();
    }

    static async delete(url) {
        const response = await this.fetch(url, {
            method: 'DELETE'
        });
        return response?.json();
    }
}

/**
 * Auto-restore session on page load
 */
document.addEventListener('DOMContentLoaded', function() {
    // Check if user was previously authenticated
    if (authStorage.isAuthenticated()) {
        console.log('User session restored from localStorage');
        // Optionally update UI to reflect logged-in state
        updateAuthUI();
    }
});

/**
 * Update UI based on authentication state
 */
function updateAuthUI() {
    const user = authStorage.getUser();
    if (user && user.username) {
        // Dispatch custom event that other parts of the application can listen to
        const event = new CustomEvent('userSessionRestored', {
            detail: { user: user }
        });
        document.dispatchEvent(event);
    }
}

console.log('Authentication storage manager loaded');
