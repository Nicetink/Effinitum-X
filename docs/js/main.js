document.addEventListener('DOMContentLoaded', function() {
    // Mobile menu toggle
    const burger = document.querySelector('.burger');
    const nav = document.querySelector('.nav-links');
    
    if (burger) {
        burger.addEventListener('click', function() {
            nav.classList.toggle('active');
            burger.classList.toggle('toggle');
        });
    }
    
    // Close mobile menu when clicking on a link
    const navLinks = document.querySelectorAll('.nav-links li');
    navLinks.forEach(link => {
        link.addEventListener('click', function() {
            if (nav.classList.contains('active')) {
                nav.classList.remove('active');
                burger.classList.remove('toggle');
            }
        });
    });
    
    // Smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            const href = this.getAttribute('href');
            
            if (href !== '#') {
                e.preventDefault();
                
                const targetElement = document.querySelector(href);
                if (targetElement) {
                    targetElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });
    });
    
    // Animation on scroll
    const animateOnScroll = function() {
        const elements = document.querySelectorAll('.feature-card, .screenshot, .version-card, .download-card');
        
        elements.forEach(element => {
            const elementPosition = element.getBoundingClientRect().top;
            const screenPosition = window.innerHeight;
            
            if (elementPosition < screenPosition - 100) {
                element.style.opacity = '1';
                element.style.transform = 'translateY(0)';
            }
        });
    };
    
    // Set initial styles for animation elements
    const setupAnimations = function() {
        const elements = document.querySelectorAll('.feature-card, .screenshot, .version-card, .download-card');
        
        elements.forEach(element => {
            element.style.opacity = '0';
            element.style.transform = 'translateY(20px)';
            element.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        });
    };
    
    // Setup and run animations
    setupAnimations();
    window.addEventListener('scroll', animateOnScroll);
    animateOnScroll(); // Run once on page load
    
    // Toggle burger animation
    if (burger) {
        burger.addEventListener('click', () => {
            const lines = burger.querySelectorAll('div');
            
            lines[0].classList.toggle('line1-active');
            lines[1].classList.toggle('line2-active');
            lines[2].classList.toggle('line3-active');
        });
    }
    
    // Add CSS for burger animation
    const style = document.createElement('style');
    style.textContent = `
        .line1-active {
            transform: rotate(-45deg) translate(-5px, 6px);
        }
        .line2-active {
            opacity: 0;
        }
        .line3-active {
            transform: rotate(45deg) translate(-5px, -6px);
        }
    `;
    document.head.appendChild(style);
}); 