// Template Designer JavaScript Interop Functions

/**
 * Reads a file as base64 data URL
 * @param {File} file - The file to read
 * @returns {Promise<string>} Base64 encoded data URL
 */
async function readFileAsBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
            // Extract base64 data from data URL (remove "data:image/png;base64," prefix)
            const base64 = reader.result.split(',')[1];
            resolve(base64);
        };
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(file);
    });
}

/**
 * Triggers a file input click programmatically
 * @param {string} inputId - The ID of the file input element
 */
function triggerFileInput(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        input.click();
    }
}

/**
 * Gets the current value of a color input
 * @param {string} inputId - The ID of the color input element
 * @returns {string} The hex color value
 */
function getColorValue(inputId) {
    const input = document.getElementById(inputId);
    return input ? input.value : '#000000';
}

/**
 * Sets the value of a color input
 * @param {string} inputId - The ID of the color input element
 * @param {string} value - The hex color value to set
 */
function setColorValue(inputId, value) {
    const input = document.getElementById(inputId);
    if (input) {
        input.value = value;
    }
}

/**
 * Downloads the signature HTML as a file
 * @param {string} html - The HTML content to download
 * @param {string} filename - The filename for the download
 */
function downloadSignatureHtml(html, filename) {
    const blob = new Blob([html], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'signature.html';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

/**
 * Validates an image file
 * @param {File} file - The file to validate
 * @param {number} maxSizeKB - Maximum file size in KB
 * @returns {object} Validation result with isValid and error properties
 */
function validateImageFile(file, maxSizeKB) {
    const result = { isValid: true, error: null };

    // Check file type
    const validTypes = ['image/png', 'image/jpeg', 'image/gif'];
    if (!validTypes.includes(file.type)) {
        result.isValid = false;
        result.error = 'Invalid file type. Please upload a PNG, JPEG, or GIF image.';
        return result;
    }

    // Check file size
    const maxSizeBytes = (maxSizeKB || 500) * 1024;
    if (file.size > maxSizeBytes) {
        result.isValid = false;
        result.error = `File is too large. Maximum size is ${maxSizeKB || 500}KB.`;
        return result;
    }

    return result;
}

// Expose functions globally for Blazor interop
window.templateDesigner = {
    readFileAsBase64,
    triggerFileInput,
    getColorValue,
    setColorValue,
    downloadSignatureHtml,
    validateImageFile
};
