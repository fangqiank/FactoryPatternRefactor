function escapeHtml(str) {
    const el = document.createElement('span');
    el.textContent = str;
    return el.innerHTML;
}

async function loadChannels() {
    try {
        const res = await fetch('/channels');
        const data = await res.json();
        const selects = [
            document.getElementById('singleChannel'),
        ];
        selects.forEach(sel => {
            sel.innerHTML = '';
            data.availableChannels.forEach(ch => {
                const opt = document.createElement('option');
                opt.value = ch;
                opt.textContent = ch;
                sel.appendChild(opt);
            });
        });
    } catch {
        console.error('Failed to load channels');
    }
}

function setResult(el, html, isError) {
    el.className = 'result ' + (isError ? 'result-error' : 'result-success');
    el.innerHTML = html;
}

function setLoading(btn, loading) {
    btn.disabled = loading;
    btn.textContent = loading ? 'Sending...' : btn.dataset.originalText || btn.textContent;
}

// --- Single Send ---
document.getElementById('singleForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('singleBtn');
    const resultEl = document.getElementById('singleResult');

    const payload = {
        channel: document.getElementById('singleChannel').value,
        recipient: document.getElementById('singleRecipient').value,
        subject: document.getElementById('singleSubject').value,
        body: document.getElementById('singleBody').value
    };

    btn.dataset.originalText = btn.textContent;
    setLoading(btn, true);

    try {
        const res = await fetch('/notify/single', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            const data = await res.json();
            setResult(resultEl, `Sent via <strong>${escapeHtml(data.channel)}</strong> to <strong>${escapeHtml(data.recipient)}</strong>`, false);
        } else {
            const err = await res.json();
            setResult(resultEl, `Error: ${escapeHtml(err.detail || err.title || res.status)}`, true);
        }
    } catch (err) {
        setResult(resultEl, `Network error: ${escapeHtml(err.message)}`, true);
    } finally {
        setLoading(btn, false);
    }
});

// --- Bulk Send ---
let bulkRowIndex = 0;

function addBulkRow() {
    const container = document.getElementById('bulkRows');
    const row = document.createElement('div');
    row.className = 'bulk-row';
    row.dataset.index = bulkRowIndex++;

    row.innerHTML = `
        <div class="form-group">
            <label>Channel</label>
            <select class="bulk-channel" required></select>
        </div>
        <div class="form-group">
            <label>Recipient</label>
            <input type="text" class="bulk-recipient" required>
        </div>
        <div class="form-group">
            <label>Subject</label>
            <input type="text" class="bulk-subject">
        </div>
        <div class="form-group">
            <label>Body</label>
            <input type="text" class="bulk-body" required>
        </div>
        <button type="button" class="btn btn-danger" onclick="removeBulkRow(this)">X</button>
    `;
    container.appendChild(row);

    // Populate channel select
    const singleSelect = document.getElementById('singleChannel');
    const bulkSelect = row.querySelector('.bulk-channel');
    bulkSelect.innerHTML = singleSelect.innerHTML;
    bulkSelect.selectedIndex = 0;
}

function removeBulkRow(btn) {
    btn.closest('.bulk-row').remove();
}

async function sendBulk() {
    const rows = document.querySelectorAll('.bulk-row');
    const btn = document.getElementById('bulkBtn');
    const resultEl = document.getElementById('bulkResult');

    if (rows.length === 0) {
        setResult(resultEl, 'Add at least one row.', true);
        return;
    }

    const payload = Array.from(rows).map(row => ({
        channel: row.querySelector('.bulk-channel').value,
        recipient: row.querySelector('.bulk-recipient').value,
        subject: row.querySelector('.bulk-subject').value,
        body: row.querySelector('.bulk-body').value
    }));

    btn.dataset.originalText = btn.textContent;
    setLoading(btn, true);

    try {
        const res = await fetch('/notify/bulk', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await res.json();

        let html = `<strong>${data.succeeded}/${data.total}</strong> sent successfully.`;
        if (data.details && data.details.length > 0) {
            html += '<table class="detail-table"><tr><th>Channel</th><th>Recipient</th><th>Status</th><th>Error</th></tr>';
            data.details.forEach(d => {
                const cls = d.success ? 'success' : 'failed';
                const status = d.success ? 'OK' : 'FAIL';
                html += `<tr><td>${escapeHtml(d.channel)}</td><td>${escapeHtml(d.recipient)}</td><td class="${cls}">${status}</td><td>${d.error ? escapeHtml(d.error) : '-'}</td></tr>`;
            });
            html += '</table>';
        }
        setResult(resultEl, html, data.failed > 0);

    } catch (err) {
        setResult(resultEl, `Network error: ${escapeHtml(err.message)}`, true);
    } finally {
        setLoading(btn, false);
    }
}

// Init
loadChannels().then(() => {
    addBulkRow();
    addBulkRow();
});
