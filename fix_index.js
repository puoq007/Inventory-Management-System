const fs = require('fs');
let c = fs.readFileSync('frontend/wwwroot/index.html', 'utf8');
// The corrupted string is: <\\/scr`ipt>
const bad = '<\\\\/scr\`ipt>';
const good = '<\\/script>';
const count = c.split(bad).length - 1;
console.log('Found:', count);
c = c.split(bad).join(good);
fs.writeFileSync('frontend/wwwroot/index.html', c, 'utf8');
console.log('Done');
