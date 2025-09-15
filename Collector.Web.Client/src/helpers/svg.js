/**
 * Loads an SVG from a URL and adds it to the SVGs container
 * @param {string} url - The URL of the SVG to load
 * @param {string} [id] - Optional custom ID for the SVG element
 * @returns {Promise<SVGElement|null>} - Promise resolving to the SVG element or null if not found
 */
const addSvg = (url, id) => {
    // Generate a unique ID based on the URL to track loaded SVGs
    const svgId = url.replace(/[^a-zA-Z0-9]/g, '-');
    
    // Check if the SVG has already been loaded by looking for an SVG with this data-url attribute
    if (document.querySelector(`svg[data-url="${url}"]`)) {
        return; // SVG already loaded, exit early
    }
    
    // Create a fetch request to get the SVG content
    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error(`Failed to load SVG: ${response.status} ${response.statusText}`);
            }
            return response.text();
        })
        .then(svgContent => {
            // Check if the svgs div already exists
            let svgsDiv = document.querySelector('div.svgs');
            
            // If it doesn't exist, create it and append to body
            if (!svgsDiv) {
                svgsDiv = document.createElement('div');
                svgsDiv.className = 'svgs';
                svgsDiv.style.display = 'none'; // Hide the SVG container
                document.body.appendChild(svgsDiv);
            }
            
            // Create a temporary div to parse the SVG content
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = svgContent.trim();
            
            // Get the SVG element
            const svgElement = tempDiv.querySelector('svg');
            
            if (svgElement) {
                // Add data attribute to track this SVG
                svgElement.setAttribute('data-url', url);
                
                // Set custom ID if provided
                if (id) {
                    svgElement.id = id;
                }
                
                // Append the SVG element directly to the svgs div
                svgsDiv.appendChild(svgElement);
                return svgElement;
            } else {
                console.error('No SVG element found in the loaded content');
                return null;
            }
        })
        .catch(error => {
            console.error('Error loading SVG:', error);
        });
};

/**
 * Gets an SVG element that was previously loaded by URL
 * @param {string} url - The URL of the SVG to retrieve
 * @returns {SVGElement|null} - The SVG element or null if not found
 */
const getSvg = (url) => {
    return document.querySelector(`svg[data-url="${url}"]`);
};

export { addSvg, getSvg };
