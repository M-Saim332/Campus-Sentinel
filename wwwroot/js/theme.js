// Immediate theme load to prevent flicker
(function() {
    const savedTheme = localStorage.getItem('theme') || 'custom';
    document.documentElement.setAttribute('data-theme', savedTheme);
})();

document.addEventListener('DOMContentLoaded', () => {
    const themeSwitcher = document.getElementById('themeSwitcher');
    
    if (themeSwitcher) {
        // Set dropdown value to current theme
        const currentTheme = document.documentElement.getAttribute('data-theme') || 'custom';
        themeSwitcher.value = currentTheme;
        
        // Listen for changes
        themeSwitcher.addEventListener('change', (e) => {
            const newTheme = e.target.value;
            document.documentElement.setAttribute('data-theme', newTheme);
            localStorage.setItem('theme', newTheme);
        });
    }
});
