window.dashboardCharts = (function () {
    const charts = {};

    function waitForChart(callback, maxAttempts = 50) {
        if (window.Chart) {
            callback();
            return;
        }
        
        let attempts = 0;
        const checkInterval = setInterval(() => {
            attempts++;
            if (window.Chart) {
                clearInterval(checkInterval);
                callback();
            } else if (attempts >= maxAttempts) {
                clearInterval(checkInterval);
                console.error('Chart.js failed to load after', maxAttempts * 100, 'ms');
            }
        }, 100);
    }

    function createOrUpdateChart(key, ctx, config) {
        if (!ctx) {
            console.warn('Canvas element not found for', key);
            return;
        }

        waitForChart(() => {
            if (!window.Chart) {
                console.error('Chart.js is still not available for', key);
                return;
            }

            if (charts[key]) {
                charts[key].destroy();
            }

            try {
                charts[key] = new Chart(ctx, config);
                console.log('Chart initialized successfully for', key);
            } catch (error) {
                console.error('Error creating chart for', key, ':', error);
            }
        });
    }

    function initRevenue(labels, data) {
        console.log('initRevenue called with', labels?.length, 'labels and', data?.length, 'data points');
        const ctx = document.getElementById('revenueChart');
        if (!ctx) {
            console.error('revenueChart canvas not found in DOM');
            return;
        }
        console.log('Found revenueChart canvas, creating chart...');
        createOrUpdateChart('revenue', ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Revenue',
                    data: data,
                    borderColor: '#234060',
                    backgroundColor: 'rgba(35, 64, 96, 0.15)',
                    tension: 0.3,
                    fill: true,
                    borderWidth: 2,
                    pointRadius: 3,
                    pointBackgroundColor: '#234060'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const value = ctx.parsed.y ?? 0;
                                return '₱' + value.toLocaleString();
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (val) {
                                return '₱' + val.toLocaleString();
                            }
                        }
                    }
                }
            }
        });
    }

    function initCheckins(labels, data) {
        console.log('initCheckins called with', labels?.length, 'labels and', data?.length, 'data points');
        const ctx = document.getElementById('checkinsChart');
        if (!ctx) {
            console.error('checkinsChart canvas not found in DOM');
            return;
        }
        console.log('Found checkinsChart canvas, creating chart...');
        createOrUpdateChart('checkins', ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Check-ins',
                    data: data,
                    borderColor: '#27ae60',
                    backgroundColor: 'rgba(39, 174, 96, 0.15)',
                    tension: 0.3,
                    fill: true,
                    borderWidth: 2,
                    pointRadius: 3,
                    pointBackgroundColor: '#27ae60'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: { beginAtZero: true, precision: 0 }
                }
            }
        });
    }

    function initMemberGrowth(labels, data) {
        console.log('initMemberGrowth called with', labels?.length, 'labels and', data?.length, 'data points');
        const ctx = document.getElementById('memberGrowthChart');
        if (!ctx) {
            console.error('memberGrowthChart canvas not found in DOM');
            return;
        }
        console.log('Found memberGrowthChart canvas, creating chart...');
        createOrUpdateChart('memberGrowth', ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'New Members',
                    data: data,
                    backgroundColor: 'rgba(37, 99, 235, 0.7)',
                    borderColor: '#2563eb',
                    borderWidth: 1.5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: { beginAtZero: true, precision: 0 }
                }
            }
        });
    }

    function initMembershipTypes(labels, data) {
        console.log('initMembershipTypes called with', labels?.length, 'labels and', data?.length, 'data points');
        const ctx = document.getElementById('membershipTypeChart');
        if (!ctx) {
            console.error('membershipTypeChart canvas not found in DOM');
            return;
        }
        console.log('Found membershipTypeChart canvas, creating chart...');
        const colors = [
            '#2563eb', '#10b981', '#f59e0b',
            '#ef4444', '#8b5cf6', '#0ea5e9'
        ];

        createOrUpdateChart('membershipTypes', ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: labels.map((_, i) => colors[i % colors.length]),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { boxWidth: 12 }
                    }
                },
                cutout: '60%'
            }
        });
    }

    function waitForCanvas(canvasId, callback, maxAttempts = 50) {
        const canvas = document.getElementById(canvasId);
        if (canvas) {
            callback();
            return;
        }
        
        let attempts = 0;
        const checkInterval = setInterval(() => {
            attempts++;
            const canvas = document.getElementById(canvasId);
            if (canvas) {
                clearInterval(checkInterval);
                callback();
            } else if (attempts >= maxAttempts) {
                clearInterval(checkInterval);
                console.error('Canvas', canvasId, 'not found after', maxAttempts * 100, 'ms');
            }
        }, 100);
    }

    return {
        initializeDashboardCharts: function (
            revenueLabels, revenueData,
            checkinsLabels, checkinsData,
            memberGrowthLabels, memberGrowthData,
            membershipTypeNames, membershipTypeCounts
        ) {
            console.log('initializeDashboardCharts called');
            console.log('Chart.js available?', typeof window.Chart !== 'undefined');
            console.log('Checking for canvas elements...');
            
            // Wait for Chart.js AND canvas elements to be ready
            waitForChart(() => {
                // Wait a bit more for DOM to settle
                setTimeout(() => {
                    try {
                        console.log('Chart.js loaded, checking canvas elements...');
                        
                        // Check if all canvas elements exist
                        const canvases = [
                            'revenueChart',
                            'checkinsChart', 
                            'memberGrowthChart',
                            'membershipTypeChart'
                        ];
                        
                        let allFound = true;
                        for (const id of canvases) {
                            if (!document.getElementById(id)) {
                                console.warn('Canvas', id, 'not found');
                                allFound = false;
                            }
                        }
                        
                        if (!allFound) {
                            console.warn('Some canvas elements not found, retrying in 500ms...');
                            setTimeout(() => {
                                initRevenue(revenueLabels, revenueData);
                                initCheckins(checkinsLabels, checkinsData);
                                initMemberGrowth(memberGrowthLabels, memberGrowthData);
                                initMembershipTypes(membershipTypeNames, membershipTypeCounts);
                            }, 500);
                        } else {
                            console.log('All canvas elements found, initializing charts...');
                            initRevenue(revenueLabels, revenueData);
                            initCheckins(checkinsLabels, checkinsData);
                            initMemberGrowth(memberGrowthLabels, memberGrowthData);
                            initMembershipTypes(membershipTypeNames, membershipTypeCounts);
                            console.log('All charts initialized');
                        }
                    } catch (e) {
                        console.error('Error initializing dashboard charts:', e);
                    }
                }, 300);
            }, 100); // Wait up to 10 seconds for Chart.js
        }
    };
})();

// Global function for Blazor interop
window.initializeDashboardCharts = window.dashboardCharts.initializeDashboardCharts;

// Helper function to check if Chart.js is loaded
window.isChartJsAvailable = function() {
    return typeof window.Chart !== 'undefined';
};


