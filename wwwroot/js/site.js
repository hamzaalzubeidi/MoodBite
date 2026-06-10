// MoodBite — Main JavaScript

// ============================================================
// THEME SYSTEM
// ============================================================
const ThemeManager = {
    STORAGE_KEY: 'moodbite-theme',
    DARK:  'dark',
    LIGHT: 'light',

    getCurrent() {
        return localStorage.getItem(this.STORAGE_KEY) ||
            (window.matchMedia('(prefers-color-scheme: dark)').matches ? this.DARK : this.LIGHT);
    },

    apply(theme) {
        const html = document.documentElement;
        if (theme === this.LIGHT) {
            html.setAttribute('data-theme', 'light');
        } else {
            html.removeAttribute('data-theme');
        }
        localStorage.setItem(this.STORAGE_KEY, theme);
        this._updateCharts(theme);
        this._updateLeafletTiles(theme);
        if (typeof lucide !== 'undefined') lucide.createIcons();
    },

    toggle() {
        const current = this.getCurrent();
        this.apply(current === this.DARK ? this.LIGHT : this.DARK);
    },

    init() {
        // Reveal body (hidden by inline <style> in <head> to prevent flash)
        document.body.style.opacity = '1';

        // Apply saved/detected theme (also done inline in <head> — this is a safety re-apply)
        this.apply(this.getCurrent());

        // Wire toggle button
        const btn = document.getElementById('themeToggle');
        if (btn) btn.addEventListener('click', () => this.toggle());

        // Follow OS preference when no saved choice
        window.matchMedia('(prefers-color-scheme: dark)')
            .addEventListener('change', e => {
                if (!localStorage.getItem(this.STORAGE_KEY)) {
                    this.apply(e.matches ? this.DARK : this.LIGHT);
                }
            });
    },

    _updateCharts(theme) {
        const isDark = theme === this.DARK;
        const gridColor = isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)';
        const textColor = isDark ? '#9A9A9A' : '#6B7280';
        if (window.Chart && Chart.instances) {
            Object.values(Chart.instances).forEach(chart => {
                if (chart.options.scales) {
                    Object.values(chart.options.scales).forEach(scale => {
                        if (scale.grid)  scale.grid.color  = gridColor;
                        if (scale.ticks) scale.ticks.color = textColor;
                    });
                }
                chart.update('none');
            });
        }
    },

    _updateLeafletTiles(theme) {
        if (window._leafletMap) {
            if (theme === 'light' && window._lightTileLayer) {
                window._darkTileLayer && window._leafletMap.removeLayer(window._darkTileLayer);
                window._lightTileLayer.addTo(window._leafletMap);
            } else if (window._darkTileLayer) {
                window._lightTileLayer && window._leafletMap.removeLayer(window._lightTileLayer);
                window._darkTileLayer.addTo(window._leafletMap);
            }
        }
    }
};

document.addEventListener('DOMContentLoaded', () => ThemeManager.init());

// ===================================================
// NAVBAR SCROLL EFFECT
// ===================================================
window.addEventListener('scroll', function () {
    const navbar = document.querySelector('.navbar-moodbite');
    if (navbar) {
        if (window.scrollY > 10) navbar.classList.add('scrolled');
        else navbar.classList.remove('scrolled');
    }
});

// ===================================================
// TAB SYSTEM
// ===================================================
function initTabs(containerId) {
    const container = document.getElementById(containerId) || document;
    const tabBtns = container.querySelectorAll('.tab-btn');
    tabBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const target = this.dataset.target;
            tabBtns.forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            container.querySelectorAll('.tab-pane').forEach(pane => {
                pane.classList.remove('active');
                if (pane.id === target) pane.classList.add('active');
            });
        });
    });
}

document.addEventListener('DOMContentLoaded', function () {
    // Init all tab containers
    document.querySelectorAll('[data-tabs-container]').forEach(container => {
        const tabBtns = container.querySelectorAll('.tab-btn');
        tabBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const target = this.dataset.target;
                tabBtns.forEach(b => b.classList.remove('active'));
                this.classList.add('active');
                container.querySelectorAll('.tab-pane').forEach(pane => {
                    pane.classList.remove('active');
                    if (pane.id === target) pane.classList.add('active');
                });
            });
        });
    });

    // Global tab groups
    const globalTabBtns = document.querySelectorAll('[data-tab-target]');
    globalTabBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const target = this.dataset.tabTarget;
            const group = this.dataset.tabGroup || 'default';
            document.querySelectorAll(`[data-tab-group="${group}"]`).forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            document.querySelectorAll(`[data-tab-pane="${group}"]`).forEach(pane => {
                pane.classList.remove('active');
                if (pane.id === target) pane.classList.add('active');
            });
        });
    });
});

// ===================================================
// INTERSECTION OBSERVER — Fade in cards on scroll
// ===================================================
document.addEventListener('DOMContentLoaded', function () {
    const animItems = document.querySelectorAll('.card-glass, .stat-card, .animate-on-scroll');
    if (!animItems.length) return;

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-in');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.08 });

    animItems.forEach(el => {
        if (!el.style.opacity) {
            el.style.opacity = '0';
            el.style.transform = 'translateY(20px)';
            el.style.transition = 'opacity 0.55s cubic-bezier(0.22,1,0.36,1), transform 0.55s cubic-bezier(0.22,1,0.36,1)';
        }
        observer.observe(el);
    });
});

// .animate-in resets the inline opacity/transform
document.addEventListener('animationstart', function (e) {
    if (e.animationName === 'fadeInUp' && e.target.style.opacity === '0') {
        e.target.style.opacity = '';
        e.target.style.transform = '';
    }
});

// ===================================================
// CHATBOT
// ===================================================
function toggleChatbot() {
    const win = document.getElementById('chatbotWindow');
    if (win) win.classList.toggle('open');
}

async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const messages = document.getElementById('chatMessages');
    if (!input || !messages) return;

    const text = input.value.trim();
    if (!text) return;

    const userMsg = document.createElement('div');
    userMsg.className = 'chat-message user';
    userMsg.textContent = text;
    messages.appendChild(userMsg);
    input.value = '';
    messages.scrollTop = messages.scrollHeight;

    const typing = document.createElement('div');
    typing.className = 'chat-message bot';
    typing.id = 'typingIndicator';
    typing.innerHTML = '<span class="spin-slow d-inline-block me-1">⟳</span> ...';
    messages.appendChild(typing);
    messages.scrollTop = messages.scrollHeight;

    try {
        const response = await fetch('/api/Chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: text })
        });
        const data = await response.json();
        typing.remove();

        const botMsg = document.createElement('div');
        botMsg.className = 'chat-message bot';

        // Always set reply text via textContent to prevent XSS
        const replySpan = document.createElement('span');
        replySpan.textContent = data.reply || '...';
        botMsg.appendChild(replySpan);

        if (data.type === 'meal_edit' && data.showPlanButton) {
            const isOnMealPlan = window.location.pathname.toLowerCase() === '/mealplan' ||
                                  window.location.pathname.toLowerCase() === '/mealplan/index';
            if (isOnMealPlan) {
                // Already viewing the plan — reload after a short delay so the user can read the reply
                setTimeout(() => window.location.reload(), 1800);
            } else {
                // Validate the URL from the server is a safe relative path before using it
                const rawUrl = typeof data.buttonUrl === 'string' && data.buttonUrl.startsWith('/')
                    ? data.buttonUrl : '/MealPlan';

                const btn = document.createElement('a');
                btn.href = rawUrl;
                btn.className = 'btn btn-sm mt-2 d-block text-center';
                btn.style.cssText = 'background:#00C896;color:#fff;border-radius:8px;font-weight:600;border:none;text-decoration:none;padding:6px 12px;';
                btn.textContent = '🍽️ ' + (data.buttonText || 'View Meal Plan');
                botMsg.appendChild(document.createElement('br'));
                botMsg.appendChild(btn);
            }
        }

        messages.appendChild(botMsg);
        messages.scrollTop = messages.scrollHeight;
    } catch {
        typing.remove();
        const errMsg = document.createElement('div');
        errMsg.className = 'chat-message bot';
        errMsg.textContent = 'حدث خطأ. حاول مرة أخرى. / Error occurred.';
        messages.appendChild(errMsg);
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const chatInput = document.getElementById('chatInput');
    if (chatInput) {
        chatInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') sendChatMessage();
        });
    }
});

// ===================================================
// SVG CIRCULAR PROGRESS RING
// ===================================================
function createProgressRing(element, percent, color = '#00C896') {
    const size = parseInt(element.dataset.size) || 120;
    const strokeWidth = parseInt(element.dataset.strokeWidth) || 10;
    const radius = (size - strokeWidth) / 2;
    const circumference = 2 * Math.PI * radius;
    const offset = circumference - (Math.min(percent, 100) / 100) * circumference;
    const isLight = document.documentElement.getAttribute('data-theme') === 'light';
    const trackColor = isLight ? '#e7e5e4' : '#262626';

    element.innerHTML = `
        <svg width="${size}" height="${size}" style="transform:rotate(-90deg)">
            <circle cx="${size/2}" cy="${size/2}" r="${radius}"
                fill="none" stroke="${trackColor}" stroke-width="${strokeWidth}"/>
            <circle cx="${size/2}" cy="${size/2}" r="${radius}"
                fill="none" stroke="${color}" stroke-width="${strokeWidth}"
                stroke-dasharray="${circumference}"
                stroke-dashoffset="${offset}"
                stroke-linecap="round"
                style="transition:stroke-dashoffset 1s ease"/>
        </svg>`;
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-progress-ring]').forEach(el => {
        const percent = parseFloat(el.dataset.percent) || 0;
        const color = el.dataset.color || '#00C896';
        createProgressRing(el, percent, color);
    });
});

// ===================================================
// AUTO-DISMISS ALERTS
// ===================================================
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert-auto-dismiss').forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.5s ease';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 500);
        }, 4000);
    });
});

// ===================================================
// MOOD SELECTOR
// ===================================================
function selectMood(btn, mood) {
    document.querySelectorAll('.mood-btn').forEach(b => b.classList.remove('selected'));
    btn.classList.add('selected');
    const input = document.getElementById('selectedMood');
    if (input) input.value = mood;
}

// ===================================================
// CALORIE CHART (Chart.js)
// ===================================================
function renderCalorieChart(canvasId, labels, data, target) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    const isDark     = document.documentElement.getAttribute('data-theme') !== 'light';
    const gridColor  = isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)';
    const labelColor = isDark ? '#9A9A9A' : '#6B7280';
    const titleColor = isDark ? '#F5F5F5' : '#111111';

    const colors = data.map(v => {
        if (v === 0) return 'rgba(0,200,150,0.15)';
        const diff = Math.abs(v - target) / target;
        if (diff <= 0.15) return '#00C896';
        if (v > target)   return '#EF4444';
        return 'rgba(245,158,11,0.72)';
    });

    window._calorieChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [{
                data,
                backgroundColor: colors,
                borderRadius: 10,
                borderSkipped: false
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: isDark ? '#2A2A2A' : '#FFFFFF',
                    titleColor,
                    bodyColor:   labelColor,
                    borderColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)',
                    borderWidth: 1,
                    callbacks: { label: c => `${c.parsed.y} cal` }
                }
            },
            scales: {
                x: {
                    grid:  { color: gridColor, drawBorder: false },
                    ticks: { color: labelColor, font: { family: 'Inter, Cairo' } }
                },
                y: {
                    grid:  { color: gridColor, drawBorder: false },
                    ticks: { color: labelColor, font: { family: 'Inter, Cairo' } },
                    beginAtZero: true
                }
            }
        }
    });
}

// ===================================================
// WEIGHT TREND CHART (Chart.js)
// ===================================================
function renderWeightChart(canvasId, labels, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    const isDark     = document.documentElement.getAttribute('data-theme') !== 'light';
    const gridColor  = isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)';
    const labelColor = isDark ? '#9A9A9A' : '#6B7280';
    const titleColor = isDark ? '#F5F5F5' : '#111111';

    window._weightChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels,
            datasets: [{
                data,
                borderColor: '#00C896',
                backgroundColor: 'rgba(0,200,150,0.07)',
                borderWidth: 2.5,
                pointBackgroundColor: '#00C896',
                pointRadius: 5,
                pointHoverRadius: 7,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: isDark ? '#2A2A2A' : '#FFFFFF',
                    titleColor,
                    bodyColor:   labelColor,
                    borderColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)',
                    borderWidth: 1
                }
            },
            scales: {
                x: {
                    grid:  { color: gridColor, drawBorder: false },
                    ticks: { color: labelColor, font: { family: 'Inter, Cairo' } }
                },
                y: {
                    grid:  { color: gridColor, drawBorder: false },
                    ticks: { color: labelColor, font: { family: 'Inter, Cairo' } }
                }
            }
        }
    });
}

// ===================================================
// CONFIRM DELETE
// ===================================================
function confirmDelete(msg) {
    return confirm(msg || 'هل أنت متأكد من الحذف؟ / Are you sure you want to delete?');
}

// ===================================================
// SHOW/HIDE PASSWORD
// ===================================================
function togglePassword(inputId, btn) {
    const input = document.getElementById(inputId);
    if (input) {
        const isText = input.type === 'text';
        input.type = isText ? 'password' : 'text';
        const icon = btn ? btn.querySelector('i') : null;
        if (icon) icon.className = isText ? 'bi bi-eye' : 'bi bi-eye-slash';
    }
}

// ===================================================
// SMOOTH SCROLL TO TOP
// ===================================================
function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}
