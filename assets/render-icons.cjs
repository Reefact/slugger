// Renders icon.svg and icon-small.svg into the two files the build packs: icon.png, the NuGet
// icon of both packages, and slugger.ico, the command's own. Chromium does the rasterising, so
// what is packed is what a browser shows of the SVG.
//
//   npm install -g playwright && npx playwright install chromium
//   NODE_PATH="$(npm root -g)" node assets/render-icons.cjs

const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');

const LARGE = fs.readFileSync(path.join(__dirname, 'icon.svg'), 'utf8');
const SMALL = fs.readFileSync(path.join(__dirname, 'icon-small.svg'), 'utf8');

const NUGET_SIZE = 256;
const ICO_SIZES = [16, 24, 32, 48, 64, 128, 256];
const LARGEST_SMALL_SIZE = 32;

async function render(page, svg, size) {
    await page.setViewportSize({ width: size, height: size });
    await page.setContent(`<body style="margin:0">${svg.replace('<svg ', `<svg width="${size}" height="${size}" `)}</body>`);

    return page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: size, height: size } });
}

// An ICO is a directory of images; since Windows Vista each one may be a whole PNG file, which
// spares writing bitmaps by hand. A width or height of 0 stands for 256.
function toIco(images) {
    const header = Buffer.alloc(6 + 16 * images.length);
    header.writeUInt16LE(0, 0);
    header.writeUInt16LE(1, 2);
    header.writeUInt16LE(images.length, 4);

    let offset = header.length;
    images.forEach(({ size, png }, index) => {
        const entry = 6 + 16 * index;
        header.writeUInt8(size % 256, entry);
        header.writeUInt8(size % 256, entry + 1);
        header.writeUInt16LE(1, entry + 4);
        header.writeUInt16LE(32, entry + 6);
        header.writeUInt32LE(png.length, entry + 8);
        header.writeUInt32LE(offset, entry + 12);
        offset += png.length;
    });

    return Buffer.concat([header, ...images.map(image => image.png)]);
}

(async () => {
    const browser = await chromium.launch();
    const page = await browser.newPage();

    fs.writeFileSync(path.join(__dirname, 'icon.png'), await render(page, LARGE, NUGET_SIZE));

    const images = [];
    for (const size of ICO_SIZES) {
        const svg = size <= LARGEST_SMALL_SIZE ? SMALL : LARGE;
        images.push({ size, png: await render(page, svg, size) });
    }
    fs.writeFileSync(path.join(__dirname, 'slugger.ico'), toIco(images));

    await browser.close();
})();
