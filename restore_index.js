const { execSync } = require('child_process');
const fs = require('fs');
const content = execSync('git show f49693f:frontend/wwwroot/index.html').toString('utf8');
fs.writeFileSync('frontend/wwwroot/index.html', content, { encoding: 'utf8' });
console.log('Done, length:', content.length);
// Verify
const check = fs.readFileSync('frontend/wwwroot/index.html', 'utf8');
const idx = check.indexOf('Loading Jig Inventory');
console.log('Loading text:', check.substring(idx, idx + 30));
