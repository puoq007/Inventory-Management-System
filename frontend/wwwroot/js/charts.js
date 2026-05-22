console.log("Jig Inventory JS Initializing...");

// ─── SweetAlert2 confirm ───────────────────────────────────────────────────
window.confirmAction = function (title, text, confirmBtnStr, icon) {
    return Swal.fire({
        title: title || 'Are you sure?',
        text: text || "You won't be able to revert this!",
        icon: icon || 'warning',
        showCancelButton: true,
        confirmButtonColor: '#2563eb',
        cancelButtonColor: '#94a3b8',
        confirmButtonText: confirmBtnStr || 'Yes'
    }).then(function (result) {
        return result.isConfirmed;
    });
};

// ─── CSV download ──────────────────────────────────────────────────────────
window.downloadTextFile = function (fileName, content) {
    var blob = new Blob(["\ufeff" + content], { type: "text/csv;charset=utf-8;" });
    var link = document.createElement("a");
    if (link.download !== undefined) {
        var url = URL.createObjectURL(blob);
        link.setAttribute("href", url);
        link.setAttribute("download", fileName);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
};

// ─── PDF export (print window) ─────────────────────────────────────────────
window.exportPdf = async function (title, htmlContent, fileName) {
    var logoHtml = '<div class="logo-text">MATTEL</div>';
    try {
        var resp = await fetch('/images/mattel.png');
        var blob = await resp.blob();
        var b64 = await new Promise(function (res) {
            var r = new FileReader();
            r.onloadend = function () { res(r.result); };
            r.readAsDataURL(blob);
        });
        logoHtml = '<img src="' + b64 + '" class="logo-img" alt="Mattel" />';
    } catch (e) {}

    var safeHtml = htmlContent.replace("<div class='logo-text'>MATTEL</div>", logoHtml);
    var printWnd = window.open('', '_blank', 'width=900,height=700');
    if (!printWnd) { alert('Pop-up blocked. Please allow pop-ups for this site.'); return; }

    printWnd.document.write('<!DOCTYPE html><html><head><meta charset="utf-8"/><title>' + title + '</title><style>' +
        '@page { size: A4; margin: 15mm; }' +
        '* { box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }' +
        'body { font-family: Arial, Helvetica, sans-serif; font-size: 10pt; color: #111; background: white; margin: 0; padding: 0; }' +
        '.pdf-header { display: flex; justify-content: space-between; align-items: flex-end; border-bottom: 2px solid #cc0000; padding-bottom: 6px; margin-bottom: 14px; }' +
        '.pdf-header .logo-text { font-size: 18pt; font-weight: 900; color: #cc0000; letter-spacing: 1px; }' +
        '.pdf-header .logo-img { height: 72px; width: auto; object-fit: contain; }' +
        '.pdf-header .meta { text-align: right; font-size: 8pt; color: #555; }' +
        'h2 { font-size: 13pt; font-weight: 800; color: #111; margin: 14px 0 6px 0; border-left: 4px solid #cc0000; padding-left: 8px; }' +
        'table { width: 100%; border-collapse: collapse; margin-bottom: 14px; font-size: 8.5pt; }' +
        'thead tr { background: #1e293b; color: white; }' +
        'thead th { padding: 6px 8px; text-align: left; font-weight: 700; font-size: 8pt; }' +
        'tbody tr:nth-child(even) { background: #f8fafc; }' +
        'tbody tr:hover { background: #f1f5f9; }' +
        'tbody td { padding: 5px 8px; border-bottom: 1px solid #e2e8f0; vertical-align: top; }' +
        '.stat-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; margin-bottom: 14px; }' +
        '.stat-card { border: 1px solid #e2e8f0; border-radius: 6px; padding: 10px 12px; }' +
        '.stat-card .label { font-size: 7.5pt; color: #64748b; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; }' +
        '.stat-card .value { font-size: 18pt; font-weight: 900; color: #111; line-height: 1.2; margin-top: 2px; }' +
        '.badge { display: inline-block; padding: 2px 7px; border-radius: 20px; font-size: 7.5pt; font-weight: 700; }' +
        '.badge-green  { background: #dcfce7; color: #166534; }' +
        '.badge-blue   { background: #dbeafe; color: #1e40af; }' +
        '.badge-amber  { background: #fef3c7; color: #92400e; }' +
        '.badge-red    { background: #fee2e2; color: #991b1b; }' +
        '.badge-purple { background: #f3e8ff; color: #6b21a8; }' +
        '.badge-gray   { background: #f1f5f9; color: #475569; }' +
        '.pdf-footer { margin-top: 20px; border-top: 1px solid #e2e8f0; padding-top: 6px; font-size: 7.5pt; color: #94a3b8; text-align: center; }' +
        '</style></head><body>' + safeHtml +
        '<scr' + 'ipt>window.onload=function(){setTimeout(function(){window.print();},400);}<\/scr' + 'ipt>' +
        '</body></html>');
    printWnd.document.close();
};

// ─── Date formatter for QR labels ─────────────────────────────────────────
function cleanDate(dString) {
    if (!dString || dString === '-') return '-';
    var d = dString.split(' ')[0].split('T')[0];
    d = d.replace(/-/g, '/');
    var parts = d.split('/');
    if (parts.length === 3) {
        var p1 = parseInt(parts[0], 10);
        var p2 = parseInt(parts[1], 10);
        var p3 = parseInt(parts[2], 10);
        var day, month, year;
        if (p1 > 31) { year = p1; month = p2; day = p3; }
        else { day = p1; month = p2; year = p3; }
        if (year >= 2500) year -= 543;
        if (year < 100) year += 2000;
        return day.toString().padStart(2, '0') + '/' + month.toString().padStart(2, '0') + '/' + year;
    }
    return d;
}

// ─── CSS template for jig QR plates (100×40 mm) ───────────────────────────
var cssTemplate =
    '@page { size: 100mm 40mm; margin: 0; }' +
    '* { box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }' +
    'html, body { margin: 0; padding: 0; background: white; font-family: Arial, Helvetica, sans-serif; }' +
    '.print-page { page-break-after: always; width: 100mm; height: 40mm; position: relative; overflow: hidden; }' +
    '.hole-left, .hole-right { position: absolute; width: 3.5mm; height: 3.5mm; border-radius: 50%; border: 0.4mm solid #bbb; background: white; z-index: 10; }' +
    '.hole-left  { left: 1.25mm;  top: 1.75mm; }' +
    '.hole-right { right: 1.25mm; bottom: 1.75mm; }' +
    '.plate { position: absolute; top: 3.5mm; left: 6.5mm; width: 87mm; height: 33mm; display: flex; flex-direction: row; align-items: center; gap: 2.5mm; }' +
    '.plate-qr { width: 29mm; height: 29mm; flex-shrink: 0; display: flex; align-items: center; justify-content: center; margin-top: 3mm; }' +
    '.plate-qr svg { width: 29mm; height: 29mm; display: block; }' +
    '.plate-info { flex: 1; display: flex; flex-direction: column; justify-content: center; gap: 0.7mm; min-width: 0; padding-left: 2mm; }' +
    '.plate-id { font-size: 11pt; font-weight: 900; color: #000; line-height: 1.1; letter-spacing: -0.3px; margin-bottom: 0.5mm; }' +
    '.plate-row { font-size: 6pt; font-weight: 400; color: #000; line-height: 1.3; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }' +
    '.plate-inline { display: flex; gap: 3mm; }';

function openPrintWnd(w, h) {
    var wnd = window.open('', '_blank', 'width=' + w + ',height=' + h);
    if (!wnd) { alert('Pop-up blocked. Please allow pop-ups for this site.'); }
    return wnd;
}

function plateHtml(svgHtml, data) {
    var dateStr = cleanDate(data.date);
    return '<div class="print-page">' +
        '<div class="hole-left"></div><div class="hole-right"></div>' +
        '<div class="plate">' +
        '<div class="plate-qr">' + svgHtml + '</div>' +
        '<div class="plate-info">' +
        '<div class="plate-id">' + data.id + '</div>' +
        '<div class="plate-row">DATE: ' + dateStr + '</div>' +
        '<div class="plate-row">STEP PRINT: ' + (data.stepPrint || '-') + '</div>' +
        '<div class="plate-row">HEIGHT JIG: ' + (data.heightJig || '-') + '</div>' +
        '<div class="plate-row plate-inline"><span>FEED: ' + (data.feed || '-') + '</span><span>SCAN: ' + (data.scan || '-') + '</span></div>' +
        '<div class="plate-row">JIG TYPE: ' + (data.jigType || '-') + '</div>' +
        '</div></div></div>';
}

// ─── Print single jig QR plate ────────────────────────────────────────────
window.printQR = function (svgHtml, data) {
    var wnd = openPrintWnd(420, 220);
    if (!wnd) return;
    wnd.document.write('<!DOCTYPE html><html><head><title>QR - ' + data.id + '</title><style>' + cssTemplate + '</style></head><body>' +
        plateHtml(svgHtml, data) +
        '<scr' + 'ipt>window.onload=function(){setTimeout(function(){window.print();window.close();},300);}<\/scr' + 'ipt>' +
        '</body></html>');
    wnd.document.close();
};

// ─── Print multiple jig QR plates ────────────────────────────────────────
window.printQRs = function (qrDataList) {
    var wnd = openPrintWnd(420, 400);
    if (!wnd) return;
    var body = '';
    qrDataList.forEach(function (data) { body += plateHtml(data.svg, data); });
    wnd.document.write('<!DOCTYPE html><html><head><title>Print Plates</title><style>' + cssTemplate + '</style></head><body>' +
        body +
        '<scr' + 'ipt>window.onload=function(){setTimeout(function(){window.print();window.close();},600);}<\/scr' + 'ipt>' +
        '</body></html>');
    wnd.document.close();
};

// ─── SVG Sheet Export — supports Mimaki UJF-3042 MkII (300×420mm) & UJF-6042 MkII (600×420mm)
// Label: 100×40mm | 3042 → 3col×10row=30/sheet | 6042 → 6col×10row=60/sheet
window.exportSVGSheets = function (qrDataList, bedW) {
    if (!qrDataList || qrDataList.length === 0) return;

    var BED_W = bedW === 600 ? 600 : 300;   // 3042=300mm, 6042=600mm
    var BED_H = 420;                         // ທั้ง  2 รุ่น แนวนอนเหมือนกัน
    var MODEL = BED_W === 600 ? 'UJF-6042 MkII' : 'UJF-3042 MkII';
    var LBL_W = 100, LBL_H = 40;
    var COLS = Math.floor(BED_W / LBL_W);    // 3042 → 3 | 6042 → 6
    var ROWS = Math.floor(BED_H / LBL_H);    // 10
    var PER_PAGE = COLS * ROWS;              // 3042 → 30 | 6042 → 60

    function escXml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // Extract inner paths from QR SVG string and scale to 29x29mm
    function extractQrPaths(svgHtml) {
        var parser = new DOMParser();
        var doc = parser.parseFromString(svgHtml, 'image/svg+xml');
        var svgEl = doc.querySelector('svg');
        if (!svgEl) return '';
        var vb = (svgEl.getAttribute('viewBox') || '0 0 100 100').split(/[\s,]+/);
        var vbW = parseFloat(vb[2]) || 100;
        var vbH = parseFloat(vb[3]) || 100;
        var QR_MM = 29;
        var sx = (QR_MM / vbW).toFixed(6);
        var sy = (QR_MM / vbH).toFixed(6);
        var qrY = ((LBL_H - QR_MM) / 2 + 3).toFixed(2);  // +3mm เลื่อนลงให้ center ตรงกับ text block
        return '<g transform="translate(6,' + qrY + ') scale(' + sx + ',' + sy + ')">' + svgEl.innerHTML + '</g>';
    }

    function buildLabel(data, x, y) {
        var dateStr  = cleanDate(data.date);
        var textX    = 38;
        var ID_FS    = 4.5;
        var ROW_FS   = 2.4;
        return [
            '<g transform="translate(' + x.toFixed(2) + ',' + y.toFixed(2) + ')">',
            '<rect width="' + LBL_W + '" height="' + LBL_H + '" fill="white"/>',
            extractQrPaths(data.svg),
            '<text x="' + textX + '" y="12" font-family="Arial,Helvetica,sans-serif" font-size="' + ID_FS + '" font-weight="900" fill="#000">' + escXml(data.id) + '</text>',
            '<text x="' + textX + '" y="18" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">DATE: ' + escXml(dateStr) + '</text>',
            '<text x="' + textX + '" y="23" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">STEP PRINT: ' + escXml(data.stepPrint || '-') + '</text>',
            '<text x="' + textX + '" y="28" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">HEIGHT JIG: ' + escXml(data.heightJig || '-') + '</text>',
            '<text x="' + textX + '" y="33" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">FEED: ' + escXml(data.feed || '-') + '   SCAN: ' + escXml(data.scan || '-') + '</text>',
            '<text x="' + textX + '" y="38" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">JIG TYPE: ' + escXml(data.jigType || '-') + '</text>',
            '</g>'
        ].join('\n');
    }

    // Split into sheets of 30
    var sheets = [];
    for (var i = 0; i < qrDataList.length; i += PER_PAGE) {
        sheets.push(qrDataList.slice(i, i + PER_PAGE));
    }

    sheets.forEach(function (items, shIdx) {
        var labelsXml = '';
        items.forEach(function (item, idx) {
            var col = idx % COLS;
            var row = Math.floor(idx / COLS);
            labelsXml += buildLabel(item, col * LBL_W, row * LBL_H) + '\n';
        });

        var svgContent = [
            '<?xml version="1.0" encoding="UTF-8"?>',
            '<!-- Mimaki ' + MODEL + ' | ' + BED_W + 'x' + BED_H + 'mm | ' + items.length + ' labels on sheet ' + (shIdx + 1) + ' -->',
            '<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink"',
            '     width="' + BED_W + 'mm" height="' + BED_H + 'mm"',
            '     viewBox="0 0 ' + BED_W + ' ' + BED_H + '" version="1.1">',
            '  <title>Jig Plates Sheet ' + (shIdx + 1) + ' — ' + MODEL + '</title>',
            '  <rect width="' + BED_W + '" height="' + BED_H + '" fill="white"/>',
            labelsXml,
            '</svg>'
        ].join('\n');

        var blob = new Blob([svgContent], { type: 'image/svg+xml;charset=utf-8' });
        var url  = URL.createObjectURL(blob);
        var a    = document.createElement('a');
        a.href   = url;
        var prefix = BED_W === 600 ? '6042' : '3042';
        a.download = sheets.length > 1
            ? prefix + '_sheet' + (shIdx + 1) + '_of' + sheets.length + '.svg'
            : prefix + '_sheet1.svg';
        a.style.display = 'none';
        document.body.appendChild(a);
        setTimeout(function () { a.click(); document.body.removeChild(a); URL.revokeObjectURL(url); }, shIdx * 900);
    });
};

// ─── Export single jig label as standalone SVG (100×40mm) ─────────────────
window.exportSingleSVG = function (svgHtml, data) {
    var LBL_W = 100, LBL_H = 40;
    var QR_MM = 29;

    function escXml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function extractQrPaths(html) {
        var parser = new DOMParser();
        var doc = parser.parseFromString(html, 'image/svg+xml');
        var svgEl = doc.querySelector('svg');
        if (!svgEl) return '';
        var vb = (svgEl.getAttribute('viewBox') || '0 0 100 100').split(/[\s,]+/);
        var vbW = parseFloat(vb[2]) || 100;
        var vbH = parseFloat(vb[3]) || 100;
        var sx = (QR_MM / vbW).toFixed(6);
        var sy = (QR_MM / vbH).toFixed(6);
        var qrY = ((LBL_H - QR_MM) / 2 + 3).toFixed(2);
        return '<g transform="translate(6,' + qrY + ') scale(' + sx + ',' + sy + ')">' + svgEl.innerHTML + '</g>';
    }

    var dateStr = cleanDate(data.date);
    var textX = 38, ID_FS = 4.5, ROW_FS = 2.4;

    var labelContent = [
        '<rect width="' + LBL_W + '" height="' + LBL_H + '" fill="white"/>',
        extractQrPaths(svgHtml),
        '<text x="' + textX + '" y="12" font-family="Arial,Helvetica,sans-serif" font-size="' + ID_FS + '" font-weight="900" fill="#000">' + escXml(data.id) + '</text>',
        '<text x="' + textX + '" y="18" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">DATE: ' + escXml(dateStr) + '</text>',
        '<text x="' + textX + '" y="23" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">STEP PRINT: ' + escXml(data.stepPrint || '-') + '</text>',
        '<text x="' + textX + '" y="28" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">HEIGHT JIG: ' + escXml(data.heightJig || '-') + '</text>',
        '<text x="' + textX + '" y="33" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">FEED: ' + escXml(data.feed || '-') + '   SCAN: ' + escXml(data.scan || '-') + '</text>',
        '<text x="' + textX + '" y="38" font-family="Arial,Helvetica,sans-serif" font-size="' + ROW_FS + '" fill="#000">JIG TYPE: ' + escXml(data.jigType || '-') + '</text>'
    ].join('\n');

    var svgContent = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<!-- Jig Label | 100x40mm | ID: ' + data.id + ' -->',
        '<svg xmlns="http://www.w3.org/2000/svg"',
        '     width="100mm" height="40mm"',
        '     viewBox="0 0 ' + LBL_W + ' ' + LBL_H + '" version="1.1">',
        '  <title>' + escXml(data.id) + '</title>',
        labelContent,
        '</svg>'
    ].join('\n');

    var blob = new Blob([svgContent], { type: 'image/svg+xml;charset=utf-8' });
    var url  = URL.createObjectURL(blob);
    var a    = document.createElement('a');
    a.href   = url;
    a.download = 'label_' + data.id.replace(/[^a-zA-Z0-9_\-]/g, '_') + '.svg';
    a.style.display = 'none';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// ─── Locator QR helpers ───────────────────────────────────────────────────
var locatorCss =
    '@page { size: A4; margin: 15mm; }' +
    '* { box-sizing: border-box; -webkit-print-color-adjust: exact; }' +
    'body { margin: 0; padding: 0; font-family: "Segoe UI", Arial, sans-serif; background: white; }' +
    '.cards-grid { display: flex; flex-direction: column; align-items: flex-start; gap: 20px; }' +
    '.card { display: inline-flex; align-items: center; gap: 20px; border: 2px solid #ccc; border-radius: 12px; padding: 20px 24px; break-inside: avoid; max-width: 100%; }' +
    '.card .qr svg { width: 150px; height: 150px; flex-shrink: 0; }' +
    '.card .info { display: flex; flex-direction: column; gap: 5px; white-space: normal; word-break: break-word; }' +
    '.card .id { font-size: 20pt; font-weight: 900; color: #000; margin-bottom: 5px; }' +
    '.card .row { font-size: 12pt; color: #111; font-weight: 600; }' +
    '.card .label { color: #333; font-weight: 700; }';

function locatorCardHtml(svgHtml, data) {
    return '<div class="card">' +
        '<div class="qr">' + svgHtml + '</div>' +
        '<div class="info">' +
        '<span class="id">' + data.id + '</span>' +
        '<div class="row"><span class="label">Site: </span><strong>' + (data.site || '-') + '</strong></div>' +
        '<div class="row"><span class="label">Cabinet: </span>' + (data.cabinet || '-') + '</div>' +
        '<div class="row"><span class="label">Shelf: </span>' + (data.shelf || '-') + '</div>' +
        '<div class="row"><span class="label">Type: </span>' + (data.type || '-') + '</div>' +
        '</div></div>';
}

window.printLocatorQR = function (svgHtml, data) {
    var wnd = openPrintWnd(800, 600);
    if (!wnd) return;
    wnd.document.write('<!DOCTYPE html><html><head><title>Print QR - ' + data.id + '</title><style>' + locatorCss + '</style></head><body>' +
        locatorCardHtml(svgHtml, data) +
        '<scr' + 'ipt>window.onload=function(){setTimeout(function(){window.print();window.close();},300);}<\/scr' + 'ipt>' +
        '</body></html>');
    wnd.document.close();
};

window.printLocatorQRs = function (qrDataList) {
    var wnd = openPrintWnd(900, 800);
    if (!wnd) return;
    var body = '';
    qrDataList.forEach(function (data) { body += locatorCardHtml(data.svg, data); });
    wnd.document.write('<!DOCTYPE html><html><head><title>Print Location QR</title><style>' + locatorCss + '</style></head><body>' +
        '<div class="cards-grid">' + body + '</div>' +
        '<scr' + 'ipt>window.onload=function(){setTimeout(function(){window.print();window.close();},600);}<\/scr' + 'ipt>' +
        '</body></html>');
    wnd.document.close();
};

// ─── User QR badge helpers ────────────────────────────────────────────────
var userBadgeCss =
    '@page { size: A4; margin: 10mm; }' +
    '* { box-sizing: border-box; -webkit-print-color-adjust: exact; }' +
    'body { margin: 0; padding: 0; font-family: "Segoe UI", Arial, sans-serif; background: white; }' +
    '.cards-grid { display: flex; flex-wrap: wrap; gap: 10px; }' +
    '.card { display: flex; align-items: center; gap: 10px; border: 1px solid #ccc; border-radius: 8px; padding: 10px 14px; width: 220px; break-inside: avoid; }' +
    '.card .qr svg { width: 70px; height: 70px; }' +
    '.card .info { display: flex; flex-direction: column; gap: 2px; }' +
    '.card .id { font-size: 13pt; font-weight: 900; color: #000; }' +
    '.card .row { font-size: 8pt; color: #111; font-weight: 600; }' +
    '.card .label { color: #333; font-weight: 700; }';

function userCardHtml(svgHtml, data) {
    return '<div class="card">' +
        '<div class="qr">' + svgHtml + '</div>' +
        '<div class="info">' +
        '<span class="id">' + data.id + '</span>' +
        '<div class="row"><span class="label">Name: </span><strong>' + (data.name || '-') + '</strong></div>' +
        '<div class="row"><span class="label">Role: </span>' + (data.role || '-') + '</div>' +
        '</div></div>';
}

window.printUserQR = function (svgHtml, data) {
    var wnd = openPrintWnd(500, 500);
    if (!wnd) return;
    wnd.document.write('<!DOCTYPE html><html><head><title>Print QR - ' + data.id + '</title><style>' + userBadgeCss + '</style></head><body>' +
        userCardHtml(svgHtml, data) +
        '<scr' + 'ipt>window.onload=function(){setTimeout(function(){window.print();window.close();},300);}<\/scr' + 'ipt>' +
        '</body></html>');
    wnd.document.close();
};

window.printUserQRs = function (qrDataList) {
    var wnd = openPrintWnd(900, 800);
    if (!wnd) return;
    var body = '';
    qrDataList.forEach(function (data) { body += userCardHtml(data.svg, data); });
    wnd.document.write('<!DOCTYPE html><html><head><title>Print User QR Badges</title><style>' + userBadgeCss + '</style></head><body>' +
        '<div class="cards-grid">' + body + '</div>' +
        '<scr' + 'ipt>window.onload=function(){setTimeout(function(){window.print();window.close();},600);}<\/scr' + 'ipt>' +
        '</body></html>');
    wnd.document.close();
};

// ─── QR Scanner ───────────────────────────────────────────────────────────
var scanner = null;

window.startQrScan = function (dotNetHelper, elementId) {
    try {
        if (scanner) { scanner.clear(); }
        scanner = new Html5QrcodeScanner(elementId, {
            fps: 10,
            qrbox: { width: 250, height: 250 },
            rememberLastUsedCamera: true,
            aspectRatio: 1.0
        });
        scanner.render(function (decodedText) {
            dotNetHelper.invokeMethodAsync('OnQrScanned', decodedText);
        });
    } catch (err) {
        console.error("QR Scanner Error:", err);
    }
};

window.stopQrScan = function () {
    if (scanner) {
        try {
            scanner.clear();
            scanner = null;
            var reader = document.getElementById('qr-reader');
            if (reader) reader.innerHTML = '';
        } catch (err) {
            console.warn("Stop QR Error (ignorable):", err);
        }
    }
};

// ─── Activity Chart (Chart.js) ────────────────────────────────────────────
var activityChartInstance = null;

window.initActivityChart = function (canvasId, labels, dataOut, dataIn, dataClean, labelOut, labelIn, labelClean) {
    // Guard: Chart.js must be loaded
    if (typeof Chart === 'undefined') {
        console.error('[Dashboard] Chart.js not loaded yet. Cannot render chart.');
        return;
    }

    var ctx = document.getElementById(canvasId);
    if (!ctx) {
        console.warn('[Dashboard] Canvas #' + canvasId + ' not found in DOM.');
        return;
    }

    // If a chart instance exists for a different canvas, destroy it
    if (activityChartInstance) {
        try {
            if (activityChartInstance.canvas !== ctx) {
                activityChartInstance.destroy();
                activityChartInstance = null;
            } else {
                // Same canvas — just update data
                activityChartInstance.data.labels = labels;
                activityChartInstance.data.datasets[0].label = labelOut || 'Check Outs';
                activityChartInstance.data.datasets[1].label = labelIn || 'Store';
                activityChartInstance.data.datasets[2].label = labelClean || 'Cleaning';
                activityChartInstance.data.datasets[0].data = dataOut;
                activityChartInstance.data.datasets[1].data = dataIn;
                activityChartInstance.data.datasets[2].data = dataClean;
                activityChartInstance.update();
                return;
            }
        } catch (e) {
            activityChartInstance = null;
        }
    }

    Chart.defaults.color = '#94a3b8';
    Chart.defaults.font.family = "'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

    activityChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: labelOut || 'Check Outs',
                    data: dataOut,
                    backgroundColor: '#818cf8',
                    hoverBackgroundColor: '#a5b4fc',
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: labelIn || 'Store',
                    data: dataIn,
                    backgroundColor: '#34d399',
                    hoverBackgroundColor: '#6ee7b7',
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: labelClean || 'Cleaning',
                    data: dataClean,
                    backgroundColor: '#fbbf24',
                    hoverBackgroundColor: '#fcd34d',
                    borderRadius: 4,
                    borderSkipped: false
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: {
                    position: 'top',
                    align: 'end',
                    labels: {
                        usePointStyle: true,
                        boxWidth: 12,
                        boxHeight: 12,
                        font: { size: 12, weight: 'bold' },
                        generateLabels: function (chart) {
                            var original = Chart.defaults.plugins.legend.labels.generateLabels;
                            var items = original.call(this, chart);
                            items.forEach(function (label) {
                                label.textDecoration = 'none';
                                label.fontColor = label.hidden ? '#475569' : '#cbd5e1';
                                if (label.hidden) {
                                    label.strokeStyle = label.fillStyle;
                                    label.fillStyle = 'transparent';
                                    label.lineWidth = 2;
                                }
                            });
                            return items;
                        }
                    },
                    onClick: function (e, legendItem, legend) {
                        var index = legendItem.datasetIndex;
                        var ci = legend.chart;
                        if (!ci.customSelection) { ci.customSelection = new Set(); }
                        if (ci.customSelection.has(index)) { ci.customSelection.delete(index); }
                        else { ci.customSelection.add(index); }
                        for (var i = 0; i < ci.data.datasets.length; i++) {
                            ci.getDatasetMeta(i).hidden = ci.customSelection.size > 0 && !ci.customSelection.has(i);
                        }
                        ci.update();
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(15,23,42,0.9)',
                    titleColor: '#f1f5f9',
                    bodyColor: '#cbd5e1',
                    borderColor: '#334155',
                    borderWidth: 1,
                    padding: 10,
                    cornerRadius: 8,
                    displayColors: true,
                    boxPadding: 4
                }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: { font: { weight: '600' } }
                },
                y: {
                    grid: { color: '#334155' },
                    beginAtZero: true,
                    ticks: { stepSize: 1, precision: 0 }
                }
            }
        }
    });

    console.log('[Dashboard] Chart rendered on #' + canvasId);
};

console.log("Jig Inventory JS Loaded.");
