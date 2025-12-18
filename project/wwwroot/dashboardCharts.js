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

    function initAttendanceSummary(labels, data) {
        console.log('initAttendanceSummary called with', labels?.length, 'labels and', data?.length, 'data points');
        const ctx = document.getElementById('attendanceSummaryChart');
        if (!ctx) {
            console.error('attendanceSummaryChart canvas not found in DOM');
            return;
        }
        console.log('Found attendanceSummaryChart canvas, creating chart...');
        createOrUpdateChart('attendanceSummary', ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Count',
                    data: data,
                    backgroundColor: [
                        'rgba(52, 152, 219, 0.7)',  // Blue for Scans Today
                        'rgba(39, 174, 96, 0.7)',  // Green for Unique Members
                        'rgba(241, 196, 15, 0.7)'  // Yellow for Total Active Members
                    ],
                    borderColor: [
                        '#3498db',
                        '#27ae60',
                        '#f1c40f'
                    ],
                    borderWidth: 2
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
                                return ctx.label + ': ' + ctx.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0,
                            stepSize: 1
                        }
                    }
                }
            }
        });
    }

    function initROI(labels, data) {
        console.log('initROI called with', labels?.length, 'labels and', data?.length, 'data points');
        
        // Wait for Chart.js to be available
        waitForChart(() => {
            // Wait a bit more for DOM to settle
            setTimeout(() => {
                const ctx = document.getElementById('roiChart');
                if (!ctx) {
                    console.error('roiChart canvas not found in DOM, retrying...');
                    // Retry after a delay
                    setTimeout(() => {
                        const retryCtx = document.getElementById('roiChart');
                        if (!retryCtx) {
                            console.error('roiChart canvas still not found after retry');
                            return;
                        }
                        console.log('Found roiChart canvas on retry, creating chart...');
                        createOrUpdateChart('roi', retryCtx, {
                            type: 'line',
                            data: {
                                labels: labels,
                                datasets: [{
                                    label: 'ROI (%)',
                                    data: data,
                                    borderColor: '#3498db',
                                    backgroundColor: 'rgba(52, 152, 219, 0.1)',
                                    tension: 0.4,
                                    fill: true,
                                    borderWidth: 3,
                                    pointRadius: 5,
                                    pointBackgroundColor: '#3498db',
                                    pointBorderColor: '#ffffff',
                                    pointBorderWidth: 2,
                                    pointHoverRadius: 7,
                                    pointHoverBackgroundColor: '#2980b9',
                                    pointHoverBorderColor: '#ffffff',
                                    pointHoverBorderWidth: 2
                                }]
                            },
                            options: {
                                responsive: true,
                                maintainAspectRatio: false,
                                plugins: {
                                    legend: {
                                        display: true,
                                        position: 'top',
                                        labels: {
                                            font: {
                                                size: 12,
                                                weight: '600'
                                            },
                                            color: '#333'
                                        }
                                    },
                                    tooltip: {
                                        callbacks: {
                                            label: function (ctx) {
                                                const value = ctx.parsed.y ?? 0;
                                                const sign = value >= 0 ? '+' : '';
                                                return `ROI: ${sign}${value.toFixed(2)}%`;
                                            }
                                        },
                                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                                        padding: 12,
                                        titleFont: {
                                            size: 14,
                                            weight: '600'
                                        },
                                        bodyFont: {
                                            size: 13
                                        }
                                    }
                                },
                                scales: {
                                    y: {
                                        beginAtZero: false,
                                        ticks: {
                                            callback: function (val) {
                                                return val.toFixed(1) + '%';
                                            },
                                            font: {
                                                size: 11
                                            }
                                        },
                                        grid: {
                                            color: 'rgba(0, 0, 0, 0.05)'
                                        },
                                        title: {
                                            display: true,
                                            text: 'ROI (%)',
                                            font: {
                                                size: 12,
                                                weight: '600'
                                            }
                                        }
                                    },
                                    x: {
                                        ticks: {
                                            font: {
                                                size: 11
                                            },
                                            maxRotation: 45,
                                            minRotation: 45
                                        },
                                        grid: {
                                            display: false
                                        }
                                    }
                                },
                                interaction: {
                                    intersect: false,
                                    mode: 'index'
                                }
                            }
                        });
                    }, 500);
                    return;
                }
                console.log('Found roiChart canvas, creating chart...');
                createOrUpdateChart('roi', ctx, {
                    type: 'line',
                    data: {
                        labels: labels,
                        datasets: [{
                            label: 'ROI (%)',
                            data: data,
                            borderColor: '#3498db',
                            backgroundColor: 'rgba(52, 152, 219, 0.1)',
                            tension: 0.4,
                            fill: true,
                            borderWidth: 3,
                            pointRadius: 5,
                            pointBackgroundColor: '#3498db',
                            pointBorderColor: '#ffffff',
                            pointBorderWidth: 2,
                            pointHoverRadius: 7,
                            pointHoverBackgroundColor: '#2980b9',
                            pointHoverBorderColor: '#ffffff',
                            pointHoverBorderWidth: 2
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                display: true,
                                position: 'top',
                                labels: {
                                    font: {
                                        size: 12,
                                        weight: '600'
                                    },
                                    color: '#333'
                                }
                            },
                            tooltip: {
                                callbacks: {
                                    label: function (ctx) {
                                        const value = ctx.parsed.y ?? 0;
                                        const sign = value >= 0 ? '+' : '';
                                        return `ROI: ${sign}${value.toFixed(2)}%`;
                                    }
                                },
                                backgroundColor: 'rgba(0, 0, 0, 0.8)',
                                padding: 12,
                                titleFont: {
                                    size: 14,
                                    weight: '600'
                                },
                                bodyFont: {
                                    size: 13
                                }
                            }
                        },
                        scales: {
                            y: {
                                beginAtZero: false,
                                ticks: {
                                    callback: function (val) {
                                        return val.toFixed(1) + '%';
                                    },
                                    font: {
                                        size: 11
                                    }
                                },
                                grid: {
                                    color: 'rgba(0, 0, 0, 0.05)'
                                },
                                title: {
                                    display: true,
                                    text: 'ROI (%)',
                                    font: {
                                        size: 12,
                                        weight: '600'
                                    }
                                }
                            },
                            x: {
                                ticks: {
                                    font: {
                                        size: 11
                                    },
                                    maxRotation: 45,
                                    minRotation: 45
                                },
                                grid: {
                                    display: false
                                }
                            }
                        },
                        interaction: {
                            intersect: false,
                            mode: 'index'
                        }
                    }
                });
            }, 100);
        });
    }

    function initNewMembers(labels, data) {
        console.log('initNewMembers called with', labels?.length, 'labels and', data?.length, 'data points');
        
        // Allow empty data - will show empty chart
        if (!labels || !data) {
            console.warn('initNewMembers: Invalid data provided');
            labels = ['No Data'];
            data = [0];
        }
        
        waitForChart(() => {
            // Multiple retry attempts
            let attempts = 0;
            const maxAttempts = 10;
            
            function tryInit() {
                attempts++;
                const ctx = document.getElementById('newMembersChart');
                if (!ctx) {
                    if (attempts < maxAttempts) {
                        console.log(`newMembersChart canvas not found, attempt ${attempts}/${maxAttempts}, retrying...`);
                        setTimeout(tryInit, 300);
                    } else {
                        console.error('newMembersChart canvas not found after', maxAttempts, 'attempts');
                    }
                    return;
                }
                console.log('Found newMembersChart canvas, creating chart...');
                createOrUpdateChart('newMembers', ctx, getNewMembersConfig(labels, data));
            }
            
            setTimeout(tryInit, 200);
        });
    }

    function getNewMembersConfig(labels, data) {
        return {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'New Members',
                    data: data,
                    borderColor: '#3498db',
                    backgroundColor: 'rgba(52, 152, 219, 0.1)',
                    tension: 0.4,
                    fill: true,
                    borderWidth: 3,
                    pointRadius: 5,
                    pointBackgroundColor: '#3498db',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: true, position: 'top' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return 'New Members: ' + ctx.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0, stepSize: 1 }
                    },
                    x: {
                        ticks: { maxRotation: 45, minRotation: 45 }
                    }
                }
            }
        };
    }

    function initExpiringMembers(labels, data) {
        console.log('initExpiringMembers called with', labels?.length, 'labels and', data?.length, 'data points');
        waitForChart(() => {
            setTimeout(() => {
                const ctx = document.getElementById('expiringMembersChart');
                if (!ctx) {
                    console.error('expiringMembersChart canvas not found, retrying...');
                    setTimeout(() => {
                        const retryCtx = document.getElementById('expiringMembersChart');
                        if (!retryCtx) return;
                        createOrUpdateChart('expiringMembers', retryCtx, getExpiringMembersConfig(labels, data));
                    }, 500);
                    return;
                }
                createOrUpdateChart('expiringMembers', ctx, getExpiringMembersConfig(labels, data));
            }, 100);
        });
    }

    function getExpiringMembersConfig(labels, data) {
        return {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Expiring Members',
                    data: data,
                    backgroundColor: 'rgba(241, 196, 15, 0.7)',
                    borderColor: '#f1c40f',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: true, position: 'top' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return 'Expiring: ' + ctx.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0, stepSize: 1 }
                    }
                }
            }
        };
    }

    function initExpiredMembers(labels, data) {
        console.log('initExpiredMembers called with', labels?.length, 'labels and', data?.length, 'data points');
        waitForChart(() => {
            setTimeout(() => {
                const ctx = document.getElementById('expiredMembersChart');
                if (!ctx) {
                    console.error('expiredMembersChart canvas not found, retrying...');
                    setTimeout(() => {
                        const retryCtx = document.getElementById('expiredMembersChart');
                        if (!retryCtx) return;
                        createOrUpdateChart('expiredMembers', retryCtx, getExpiredMembersConfig(labels, data));
                    }, 500);
                    return;
                }
                createOrUpdateChart('expiredMembers', ctx, getExpiredMembersConfig(labels, data));
            }, 100);
        });
    }

    function getExpiredMembersConfig(labels, data) {
        return {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Expired Members',
                    data: data,
                    backgroundColor: 'rgba(231, 76, 60, 0.7)',
                    borderColor: '#e74c3c',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: true, position: 'top' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return 'Expired: ' + ctx.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0, stepSize: 1 }
                    }
                }
            }
        };
    }

    function initLeadsOverTime(labels, leadsData, convertedData) {
        console.log('initLeadsOverTime called with', labels?.length, 'labels');
        
        if (!labels || !leadsData) {
            console.warn('initLeadsOverTime: Invalid data provided');
            labels = ['No Data'];
            leadsData = [0];
            convertedData = [0];
        }
        
        if (!convertedData) {
            convertedData = new Array(leadsData.length).fill(0);
        }
        
        waitForChart(() => {
            let attempts = 0;
            const maxAttempts = 10;
            
            function tryInit() {
                attempts++;
                const ctx = document.getElementById('leadsOverTimeChart');
                if (!ctx) {
                    if (attempts < maxAttempts) {
                        console.log(`leadsOverTimeChart canvas not found, attempt ${attempts}/${maxAttempts}, retrying...`);
                        setTimeout(tryInit, 300);
                    } else {
                        console.error('leadsOverTimeChart canvas not found after', maxAttempts, 'attempts');
                    }
                    return;
                }
                console.log('Found leadsOverTimeChart canvas, creating chart...');
                createOrUpdateChart('leadsOverTime', ctx, getLeadsOverTimeConfig(labels, leadsData, convertedData));
            }
            
            setTimeout(tryInit, 200);
        });
    }

    function getLeadsOverTimeConfig(labels, leadsData, convertedData) {
        const datasets = [{
            label: 'Leads',
            data: leadsData,
            backgroundColor: 'rgba(35, 64, 96, 0.1)',
            borderColor: '#234060',
            borderWidth: 2,
            fill: true,
            tension: 0.4
        }];
        
        if (convertedData && convertedData.length > 0) {
            datasets.push({
                label: 'Converted Members',
                data: convertedData,
                backgroundColor: 'rgba(46, 125, 50, 0.1)',
                borderColor: '#2e7d32',
                borderWidth: 2,
                fill: true,
                tension: 0.4
            });
        }
        
        return {
            type: 'line',
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { 
                        display: true, 
                        position: 'top' 
                    },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return ctx.dataset.label + ': ' + ctx.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { 
                            precision: 0, 
                            stepSize: 1 
                        }
                    }
                }
            }
        };
    }

    function initLeadsByStatus(labels, data, colors) {
        console.log('initLeadsByStatus called');
        waitForChart(() => {
            setTimeout(() => {
                const ctx = document.getElementById('leadsByStatusChart');
                if (!ctx) {
                    setTimeout(() => {
                        const retryCtx = document.getElementById('leadsByStatusChart');
                        if (!retryCtx) return;
                        createOrUpdateChart('leadsByStatus', retryCtx, getDonutChartConfig(labels, data, colors, 'Leads by Status'));
                    }, 500);
                    return;
                }
                createOrUpdateChart('leadsByStatus', ctx, getDonutChartConfig(labels, data, colors, 'Leads by Status'));
            }, 100);
        });
    }

    function initLeadsBySource(labels, data, colors) {
        console.log('initLeadsBySource called');
        waitForChart(() => {
            setTimeout(() => {
                const ctx = document.getElementById('leadsBySourceChart');
                if (!ctx) {
                    setTimeout(() => {
                        const retryCtx = document.getElementById('leadsBySourceChart');
                        if (!retryCtx) return;
                        createOrUpdateChart('leadsBySource', retryCtx, getDonutChartConfig(labels, data, colors, 'Leads by Source'));
                    }, 500);
                    return;
                }
                createOrUpdateChart('leadsBySource', ctx, getDonutChartConfig(labels, data, colors, 'Leads by Source'));
            }, 100);
        });
    }

    function initLostLeads(labels, data) {
        console.log('initLostLeads called');
        waitForChart(() => {
            setTimeout(() => {
                const ctx = document.getElementById('lostLeadsChart');
                if (!ctx) {
                    setTimeout(() => {
                        const retryCtx = document.getElementById('lostLeadsChart');
                        if (!retryCtx) return;
                        createOrUpdateChart('lostLeads', retryCtx, getLostLeadsConfig(labels, data));
                    }, 500);
                    return;
                }
                createOrUpdateChart('lostLeads', ctx, getLostLeadsConfig(labels, data));
            }, 100);
        });
    }

    function getDonutChartConfig(labels, data, colors, title) {
        return {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors,
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            label: function(ctx) {
                                const label = ctx.label || '';
                                const value = ctx.parsed || 0;
                                const total = ctx.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                return label + ': ' + value + ' (' + percentage + '%)';
                            }
                        }
                    }
                }
            }
        };
    }

    function getLostLeadsConfig(labels, data) {
        return {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Lost Leads',
                    data: data,
                    backgroundColor: 'rgba(198, 40, 40, 0.7)',
                    borderColor: '#c62828',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return 'Lost: ' + ctx.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0,
                            stepSize: 1
                        }
                    }
                }
            }
        };
    }

    return {
        initAttendanceSummary: initAttendanceSummary,
        initROIChart: initROI,
        initNewMembersChart: initNewMembers,
        initExpiringMembersChart: initExpiringMembers,
        initExpiredMembersChart: initExpiredMembers,
        initLeadsOverTimeChart: initLeadsOverTime,
        initLeadsByStatusChart: initLeadsByStatus,
        initLeadsBySourceChart: initLeadsBySource,
        initLostLeadsChart: initLostLeads,
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

// Global function for attendance summary chart
window.initAttendanceSummaryChart = function(labels, data) {
    window.dashboardCharts.initAttendanceSummary(labels, data);
};

// Global function for ROI chart
window.initROIChart = function(labels, data) {
    window.dashboardCharts.initROIChart(labels, data);
};

// Global function for new members chart
window.initNewMembersChart = function(labels, data) {
    window.dashboardCharts.initNewMembersChart(labels, data);
};

// Global function for expiring members chart
window.initExpiringMembersChart = function(labels, data) {
    window.dashboardCharts.initExpiringMembersChart(labels, data);
};

// Global function for expired members chart
window.initExpiredMembersChart = function(labels, data) {
    window.dashboardCharts.initExpiredMembersChart(labels, data);
};

// Global function for leads over time chart
window.initLeadsOverTimeChart = function(labels, leadsData, convertedData) {
    window.dashboardCharts.initLeadsOverTimeChart(labels, leadsData, convertedData);
};

// Global function for leads by status chart
window.initLeadsByStatusChart = function(labels, data, colors) {
    window.dashboardCharts.initLeadsByStatusChart(labels, data, colors);
};

// Global function for leads by source chart
window.initLeadsBySourceChart = function(labels, data, colors) {
    window.dashboardCharts.initLeadsBySourceChart(labels, data, colors);
};

// Global function for lost leads chart
window.initLostLeadsChart = function(labels, data) {
    window.dashboardCharts.initLostLeadsChart(labels, data);
};

// Helper function to check if Chart.js is loaded
window.isChartJsAvailable = function() {
    return typeof window.Chart !== 'undefined';
};


