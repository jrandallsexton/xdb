# Third-party components and licensing notes

This folder belongs to the legacy jQuery web UI ("XDB"). The UI bundles the third-party libraries below.
Each library keeps its original header; this note lists the licenses and the open items.

| component | version | where | license |
|---|---|---|---|
| jQuery | 1.8.3 | `js/xdb.core.*.min.js` (lines 1-2) | MIT (jquery.org/license) |
| jQuery UI | 1.9.2 | core bundle (lines 3-7), `css/jquery-ui-1.9.2.custom.min.css`, `css/images/ui-*` | MIT |
| JSON2 | - | core bundle (line 8) | public domain |
| jQuery BlockUI | - | core bundle (line 9) | MIT / GPL dual |
| CKEditor | 3.6.3 (rev 7474) | `ckeditor/` | GPL / LGPL / MPL tri-license, see `ckeditor/LICENSE.html` (official text from the 3.6.3 release archive) |
| Highcharts | 2.3.5 | NOT vendored; loaded from `https://code.highcharts.com/2.3.5/highcharts.js` by `dash.html` and `reporting/charts2.html` | Highcharts license (free for non-commercial use, CC BY-NC 3.0; commercial use needs a Highsoft license) - see highcharts.com/license |
| JavaScript InfoVis Toolkit | 2.0.1 | `js/jit.2.0.1.min.js` | MIT |
| Google Maps InfoBox | packed | `js/infobox_packed.js` | Apache 2.0 (only loaded when `MAPS_API_KEY` is set) |
| jqGrid | 4.4.4 | `js/jquery.jqGrid.src.min.js`, `js/grid.locale-en.min.js`, `css/ui.jqgrid.min.css` | MIT / GPL dual |
| jsTree | 1.0 | `js/jquery.jstree.min.js`, `js/themes/default/*` | MIT / GPL dual |
| Dynatree | - | `js/jquery.dynatree.min.js` | MIT / GPL dual |
| blueimp jQuery File Upload (+ UI, iframe transport) | 5.x/6.x | `js/jquery.fileupload*.min.js`, `js/jquery.iframe-transport.min.js`, `css/jquery.fileupload-ui.min.css`, `images/pbar-ani.gif` | MIT |
| jQuery lightBox | 0.5 | `js/jquery.lightbox-0.5.min.js`, `css/jquery.lightbox-0.5.min.css`, `images/lightbox-*.gif` | CC BY-SA 2.5 BR (see file header) |
| jQuery Cookies | 2.2.0 | `js/jquery.cookies.2.2.0.min.js` | MIT / GPL / BSD |
| jQuery Watermark | 3.1.4 | `js/jquery.watermark.min.js` | MIT / GPL dual |
| jQueryRotate | 2.2 | `js/jQueryRotateCompressed.2.2.js` | MIT |
| jQuery Growl | 1.0.2 | `js/jquery.growl.min.js` | unknown (no header) |
| jQuery Templates (tmpl) | beta | `js/jquery.tmpl.min.js` | MIT / GPL dual |
| jQuery context menu plugin | - | `js/jquery.contextmenu.min.js`, `css/images/contextmenu/*` | unknown (no header) |
| jQuery UI Multiselect | - | `js/ui.multiselect.min.js`, `css/ui.multiselect.min.css` | MIT / GPL dual |
| jQuery multi-accordion | 1.5.3 | `js/jquery.multi-accordion-1.5.3.min.js` | unknown (no header) |
| Underscore.js | 1.4.3 | `js/underscore-min.js` | MIT |
| Sliding panel images (web-kreation) | - | `images/slider/*`, styles folded into `css/xdb.filterpanel.*.min.css` | GPL (upstream demo shipped a GPL `copying.txt`) |
| JIT RGraph example stylesheet | - | `reporting/css/RGraph.css` | MIT (InfoVis Toolkit example) |

## Open licensing items

- **Icon sprites `images/xdb.sp.24.png` and `images/xdb.sp.32.png`** (menu, toolbar and asset-type icons), the
  context-menu icons in `images/ctx/`, and the loader/notification frame images (`images/ajax-loader*.gif`,
  `images/loader-16.gif`, `images/growl/dm_*.png`, `images/fava.png`) are stock icons of unknown provenance.
  Their license has not been established. Replace them, or confirm their license, before relying on them.
- jQuery Growl, the context menu plugin and multi-accordion ship without a license header; their upstream licenses
  were not verified.
- Highcharts is not open source; it is referenced from the vendor CDN only and is not redistributed here.
