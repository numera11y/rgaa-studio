using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using RgaaStudio.Core.Audits;
using RgaaStudio.Core.Navigation;

namespace RgaaStudio.Desktop;

public partial class MainWindow : Window
{
    private const string ReadAxeElementScript = """
        (() => {
          let element;
          try { element = document.querySelector(__TARGET__) } catch { }
          if (!element) return JSON.stringify({ missing: true });
          const rect = element.getBoundingClientRect();
          const clone = element.cloneNode(true);
          for (const field of [clone, ...clone.querySelectorAll('input,textarea,select')]) {
            field.removeAttribute('value');
            field.removeAttribute('checked');
            field.removeAttribute('selected');
            if (field.tagName === 'TEXTAREA') field.textContent = '';
          }
          for (const node of [clone, ...clone.querySelectorAll('*')]) {
            for (const attribute of [...node.attributes]) {
              if (!/^(href|src|action)$/i.test(attribute.name) || !/[?&](token|key|secret|auth|session|code)=/i.test(attribute.value)) continue;
              try {
                const url = new URL(attribute.value, location.href);
                for (const key of [...url.searchParams.keys()])
                  if (/token|key|secret|auth|session|code/i.test(key)) url.searchParams.set(key, '[redacted]');
                node.setAttribute(attribute.name, url.href);
              } catch { node.removeAttribute(attribute.name); }
            }
          }
          const technologies = [];
          const add = (name, detected) => { if (detected && !technologies.includes(name)) technologies.push(name); };
          const generator = document.querySelector('meta[name="generator" i]')?.content;
          if (generator) add(`Generator : ${generator}`, true);
          add('WordPress', !!window.wp || !!document.querySelector('link[href*="wp-content"],script[src*="wp-content"]'));
          add('Drupal', !!window.Drupal || !!document.querySelector('script[src*="/sites/default/files"],meta[name="Generator"][content*="Drupal"]'));
          add('Shopify', !!window.Shopify || !!document.querySelector('script[src*="cdn.shopify.com"]'));
          add('React', !!window.React || !!document.querySelector('[data-reactroot],[data-reactid]'));
          add('Vue', !!window.Vue || !!document.querySelector('[data-v-app],script[src*="vue"]'));
          add('Angular', !!window.ng || !!document.querySelector('[ng-version]'));
          add('jQuery', !!window.jQuery);
          add('Next.js', !!window.__NEXT_DATA__ || !!document.querySelector('script[src*="/_next/"]'));
          add('Google Tag Manager', !!window.google_tag_manager || !!document.querySelector('script[src*="googletagmanager.com/gtm.js"]'));
          add('Matomo', !!window._paq || !!document.querySelector('script[src*="matomo"]'));
          return JSON.stringify({
            missing: false,
            tag: element.tagName.toLowerCase(),
            text: (element.innerText || element.getAttribute('aria-label') || '').trim().replace(/\s+/g, ' ').slice(0, 500),
            html: clone.outerHTML.slice(0, 12000),
            x: rect.x, y: rect.y, width: rect.width, height: rect.height,
            viewportWidth: innerWidth, viewportHeight: innerHeight, technologies
          });
        })()
        """;

    private const string BuildPageProfileScript = """
        (() => {
          const safeText = (value, limit = 500) => (value || '').trim().replace(/\s+/g, ' ').slice(0, limit);
          const meta = name => safeText(document.querySelector(`meta[name="${name}" i]`)?.content);
          const property = name => safeText(document.querySelector(`meta[property="${name}" i]`)?.content);
          const safeUrl = value => {
            if (!value) return '';
            try { const url = new URL(value, location.href); return url.origin + url.pathname; }
            catch { return ''; }
          };
          const globals = [];
          for (const name of ['React','Vue','Angular','ng','Drupal','drupalSettings','Shopify','prestashop','jQuery','Alpine','Swiper','htmx','__NEXT_DATA__','__NUXT__','google_tag_manager','gtag','_paq','hj','clarity','Stripe','requirejs'])
            if (window[name] !== undefined && window[name] !== null) globals.push(name);
          const schemaTypes = new Set();
          for (const element of document.querySelectorAll('script[type="application/ld+json"]')) {
            try {
              const values = [JSON.parse(element.textContent || 'null')];
              while (values.length) {
                const value = values.pop();
                if (!value || typeof value !== 'object') continue;
                if (value['@type']) for (const type of Array.isArray(value['@type']) ? value['@type'] : [value['@type']]) schemaTypes.add(String(type));
                if (Array.isArray(value)) values.push(...value);
                else if (Array.isArray(value['@graph'])) values.push(...value['@graph']);
              }
            } catch { }
          }
          const anchors = [...document.querySelectorAll('a[href]')];
          const externalLinks = anchors.filter(anchor => {
            try { return new URL(anchor.href, location.href).origin !== location.origin; }
            catch { return false; }
          }).length;
          const images = [...document.images];
          return JSON.stringify({
            title: document.title || '',
            url: location.origin + location.pathname,
            language: document.documentElement.lang || '',
            generator: meta('generator'),
            html: (document.documentElement?.outerHTML || '').slice(0, 700000),
            globals,
            scriptCount: document.scripts.length,
            externalScriptCount: document.querySelectorAll('script[src]').length,
            stylesheetCount: document.querySelectorAll('link[rel~="stylesheet"]').length,
            imageCount: images.length,
            formCount: document.forms.length,
            seo: {
              title: document.title || '',
              description: meta('description'),
              canonical: safeUrl(document.querySelector('link[rel="canonical"]')?.href || ''),
              robots: meta('robots'),
              language: document.documentElement.lang || '',
              charset: document.characterSet || '',
              viewport: meta('viewport'),
              openGraphTitle: property('og:title'),
              openGraphDescription: property('og:description'),
              openGraphImage: safeUrl(property('og:image')),
              twitterCard: meta('twitter:card'),
              twitterTitle: meta('twitter:title'),
              h1Count: document.querySelectorAll('h1').length,
              headingCount: document.querySelectorAll('h1,h2,h3,h4,h5,h6').length,
              imageCount: images.length,
              imagesMissingAlt: images.filter(image => !image.hasAttribute('alt')).length,
              linkCount: anchors.length,
              externalLinkCount: externalLinks,
              internalLinkCount: anchors.length - externalLinks,
              structuredDataTypes: [...schemaTypes]
            }
          });
        })()
        """;

    private const string StartContrastPipetteScript = """
        (() => {
          if (window.__rgaaContrastPipetteStop) window.__rgaaContrastPipetteStop();
          let hovered = null;
          let oldOutline = '';
          let oldOffset = '';
          const restore = () => {
            if (hovered) {
              hovered.style.outline = oldOutline;
              hovered.style.outlineOffset = oldOffset;
            }
            hovered = null;
          };
          const pathFor = element => {
            if (element.id) return '#' + CSS.escape(element.id);
            const path = [];
            for (let current = element; current && current.nodeType === 1 && current !== document.documentElement; current = current.parentElement) {
              let part = current.tagName.toLowerCase();
              if (current.parentElement) {
                const siblings = [...current.parentElement.children].filter(sibling => sibling.tagName === current.tagName);
                if (siblings.length > 1) part += `:nth-of-type(${siblings.indexOf(current) + 1})`;
              }
              path.unshift(part);
            }
            return path.join(' > ');
          };
          const move = event => {
            const element = document.elementFromPoint(event.clientX, event.clientY);
            if (!element || element === document.documentElement || element === document.body || element === hovered) return;
            restore();
            hovered = element;
            oldOutline = element.style.outline;
            oldOffset = element.style.outlineOffset;
            element.style.outline = '2px solid #6F42C1';
            element.style.outlineOffset = '1px';
          };
          const pick = event => {
            if (!hovered) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            const element = hovered;
            const layers = [];
            const images = [];
            const opacity = [];
            let hasBorder = false;
            let borderColor = '';
            for (let current = element; current; current = current.parentElement) {
              const style = getComputedStyle(current);
              layers.push(style.backgroundColor);
              if (style.backgroundImage && style.backgroundImage !== 'none') images.push(current.tagName.toLowerCase());
              const currentOpacity = Number.parseFloat(style.opacity);
              if (Number.isFinite(currentOpacity) && currentOpacity < 0.999) opacity.push(currentOpacity.toString());
              if (current === element && Number.parseFloat(style.borderTopWidth) > 0 && style.borderTopColor !== 'transparent') {
                hasBorder = true;
                borderColor = style.borderTopColor;
              }
            }
            layers.reverse();
            const style = getComputedStyle(element);
            const text = (element.innerText || '').trim().replace(/\s+/g, ' ').slice(0, 240);
            const sample = {
              type: 'contrast-pick',
              tag: element.tagName.toLowerCase(),
              selector: pathFor(element),
              text,
              foreground: style.color,
              backgroundLayers: layers,
              backgroundImages: [...new Set(images)].join(', '),
              opacityLayers: [...new Set(opacity)].join(', '),
              borderColor,
              hasBorder,
              fontSize: style.fontSize,
              fontWeight: style.fontWeight
            };
            window.__rgaaContrastPipettePicked = JSON.stringify(sample);
            if (typeof window.invokeCSharpAction === 'function') window.invokeCSharpAction(JSON.stringify(sample));
            stop();
          };
          const cancel = event => {
            if (event.key !== 'Escape') return;
            event.preventDefault();
            event.stopImmediatePropagation();
            if (typeof window.invokeCSharpAction === 'function')
              window.invokeCSharpAction(JSON.stringify({ type: 'contrast-cancel' }));
            stop();
          };
          function stop() {
            document.removeEventListener('pointermove', move, true);
            document.removeEventListener('click', pick, true);
            document.removeEventListener('keydown', cancel, true);
            restore();
            window.__rgaaContrastPipetteActive = false;
          }
          window.__rgaaContrastPipetteActive = true;
          window.__rgaaContrastPipetteStop = stop;
          window.__rgaaContrastPipettePicked = '';
          document.addEventListener('pointermove', move, true);
          document.addEventListener('click', pick, true);
          document.addEventListener('keydown', cancel, true);
          return true;
        })()
        """;

    private const string ReplaceMediaWithBadgesScript = """
        (() => {
          const items = [...document.querySelectorAll('img,video,audio')];
          for (const element of items) {
            const tag = element.tagName.toLowerCase();
            const original = element.outerHTML;
            const badge = document.createElement('button');
            badge.type = 'button';
            badge.textContent = `<${tag}>`;
            badge.setAttribute('aria-label', `Prévisualiser l’élément ${tag}`);
            badge.title = 'Cliquer pour prévisualiser cet élément et son code source';
            badge.style.cssText = 'display:inline-flex;align-items:center;margin:3px;padding:4px 8px;border:1px solid #7B8190;border-radius:4px;background:#FFFDF7;color:#171F3B;font:12px sans-serif;cursor:pointer;';
            badge.addEventListener('click', event => {
              event.preventDefault();
              event.stopPropagation();
              let preview = null;
              try {
                const width = tag === 'img' ? element.naturalWidth : element.videoWidth;
                const height = tag === 'img' ? element.naturalHeight : element.videoHeight;
                if (tag !== 'audio' && width > 0 && height > 0 && width * height <= 16000000) {
                  const scale = Math.min(1, 512 / width, 320 / height);
                  const canvas = document.createElement('canvas');
                  canvas.width = Math.max(1, Math.round(width * scale));
                  canvas.height = Math.max(1, Math.round(height * scale));
                  canvas.getContext('2d').drawImage(element, 0, 0, canvas.width, canvas.height);
                  const image = canvas.toDataURL('image/png');
                  if (image.length <= 800000) preview = image;
                }
              } catch (_) { }
              const source = original.length > 12000 ? original.slice(0, 12000) + '…' : original;
              if (typeof window.invokeCSharpAction === 'function')
                window.invokeCSharpAction(JSON.stringify({ type: 'media-preview', tag, source, preview }));
            });
            element.replaceWith(badge);
          }
          return items.length;
        })()
        """;

    private const string BuildSemanticTreeScript = """
        (() => {
          const root = document.body || document.documentElement;
          function labelledName(element) {
            const label = element.getAttribute('aria-label');
            if (label && label.trim()) return label.trim();
            const labelledby = element.getAttribute('aria-labelledby');
            if (labelledby) {
              const text = labelledby.split(/\s+/).map(id => document.getElementById(id)?.innerText || '').join(' ').trim();
              if (text) return text;
            }
            const title = element.getAttribute('title');
            return title && title.trim() ? title.trim() : '';
          }
          const roleOf = element => {
            const explicit = (element.getAttribute('role') || '').trim().split(/\s+/)[0];
            if (explicit) return explicit;
            const tag = element.tagName.toLowerCase();
            if (/^h[1-6]$/.test(tag)) return 'heading';
            if (tag === 'a' && element.hasAttribute('href')) return 'link';
            if (tag === 'button' || tag === 'summary') return 'button';
            if (tag === 'nav') return 'navigation';
            if (tag === 'main') return 'main';
            if (tag === 'header' && element.parentElement === document.body) return 'banner';
            if (tag === 'footer' && element.parentElement === document.body) return 'contentinfo';
            if (tag === 'aside') return 'complementary';
            if (tag === 'section') return labelledName(element) ? 'region' : 'generic';
            if (tag === 'form') return labelledName(element) ? 'form' : 'generic';
            if (tag === 'ul' || tag === 'ol' || tag === 'menu') return 'list';
            if (tag === 'li') return 'listitem';
            if (tag === 'img') return element.getAttribute('alt') === '' ? 'presentation' : 'img';
            if (tag === 'textarea') return 'textbox';
            if (tag === 'select') return element.multiple ? 'listbox' : 'combobox';
            if (tag === 'table') return 'table';
            if (tag === 'tr') return 'row';
            if (tag === 'th') return element.scope === 'row' ? 'rowheader' : 'columnheader';
            if (tag === 'td') return 'cell';
            if (tag === 'input') {
              const type = (element.type || 'text').toLowerCase();
              if (type === 'hidden') return 'presentation';
              if (type === 'checkbox') return 'checkbox';
              if (type === 'radio') return 'radio';
              if (type === 'button' || type === 'submit' || type === 'reset') return 'button';
              if (type === 'search') return 'searchbox';
              return 'textbox';
            }
            if (tag === 'progress') return 'progressbar';
            if (tag === 'meter') return 'meter';
            if (tag === 'dialog') return element.getAttribute('aria-modal') === 'true' ? 'dialog (modal)' : 'dialog (non-modal)';
            return 'generic';
          };
          function accessibleName(element, role) {
            const explicitName = labelledName(element);
            if (explicitName) return explicitName;
            if (element.tagName.toLowerCase() === 'img' && element.hasAttribute('alt')) return element.getAttribute('alt').trim();
            if (element.id) {
              const labelElement = document.querySelector(`label[for="${CSS.escape(element.id)}"]`);
              if (labelElement?.innerText.trim()) return labelElement.innerText.trim();
            }
            if (element.labels?.length) {
              const text = [...element.labels].map(item => item.innerText).join(' ').trim();
              if (text) return text;
            }
            if (element.value && ['input','textarea'].includes(element.tagName.toLowerCase())) return element.value.trim();
            if (role === 'generic') {
              return [...element.childNodes].filter(node => node.nodeType === Node.TEXT_NODE)
                .map(node => node.textContent || '').join(' ').trim().replace(/\s+/g, ' ');
            }
            if (['link','button','heading','listitem','option','menuitem','tab'].includes(role))
              return (element.innerText || '').trim().replace(/\s+/g, ' ');
            return '';
          }

          const hidden = element => element.hidden || element.getAttribute('aria-hidden') === 'true' ||
            element.getAttribute('type') === 'hidden' || getComputedStyle(element).display === 'none' ||
            getComputedStyle(element).visibility === 'hidden';
          const semanticSelector = 'header,nav,main,footer,aside,section,article,h1,h2,h3,h4,h5,h6,address,search,form,fieldset,legend,ul,ol,menu,li,figure,figcaption,details,summary,table,caption,thead,tbody,tfoot,tr,th,td';
          const semanticElements = [...document.querySelectorAll(semanticSelector)].filter(element => !hidden(element));
          const issueByElement = new Map();
          const addIssue = (element, ruleId, message, advisory = false) => {
            if (!issueByElement.has(element)) issueByElement.set(element, { ruleId, message, advisory });
          };
          const mainElements = semanticElements.filter(element => element.tagName.toLowerCase() === 'main');
          mainElements.slice(1).forEach(element => addIssue(element, 'html5.main.multiple', 'Le document contient plusieurs éléments <main> visibles.'));
          for (const element of semanticElements) {
            const tag = element.tagName.toLowerCase();
            if (tag === 'main' && element.parentElement?.closest('main,header,nav,article,aside,footer'))
              addIssue(element, 'html5.main.nesting', '<main> ne doit pas être placé dans un autre repère ou élément de contenu.');
            if (tag === 'figcaption' && element.parentElement?.tagName.toLowerCase() !== 'figure')
              addIssue(element, 'html5.figcaption.parent', '<figcaption> doit être enfant direct d’un élément <figure>.');
            if (tag === 'li' && !['ul','ol','menu'].includes(element.parentElement?.tagName.toLowerCase()))
              addIssue(element, 'html5.li.parent', '<li> doit être placé dans une liste <ul>, <ol> ou <menu>.');
            if (tag === 'summary' && (element.parentElement?.tagName.toLowerCase() !== 'details' || element.parentElement.firstElementChild !== element))
              addIssue(element, 'html5.summary.parent', '<summary> doit être le premier enfant d’un élément <details>.');
            if (tag === 'caption' && element.parentElement?.tagName.toLowerCase() !== 'table')
              addIssue(element, 'html5.caption.parent', '<caption> doit être enfant direct d’un tableau <table>.');
            if (['th','td'].includes(tag) && element.parentElement?.tagName.toLowerCase() !== 'tr')
              addIssue(element, 'html5.cell.parent', `<${tag}> doit être placé dans une ligne <tr>.`);
            if (tag === 'tr' && !['table','thead','tbody','tfoot'].includes(element.parentElement?.tagName.toLowerCase()))
              addIssue(element, 'html5.tr.parent', '<tr> doit être placé dans un tableau ou une section de tableau.');
            if (['thead','tbody','tfoot'].includes(tag) && element.parentElement?.tagName.toLowerCase() !== 'table')
              addIssue(element, 'html5.table-section.parent', `<${tag}> doit être enfant direct d’un tableau <table>.`);
          }
          let previousHeading = 0;
          let headingIndex = 0;
          for (const heading of semanticElements.filter(element => /^h[1-6]$/i.test(element.tagName))) {
            const level = Number(heading.tagName.slice(1));
            if (previousHeading && level > previousHeading + 1)
              addIssue(heading, `html5.heading.level-skip.${headingIndex}`, `Le niveau de titre passe de H${previousHeading} à H${level}. Vérifier la hiérarchie.`, true);
            previousHeading = level;
            headingIndex++;
          }

          const semanticSet = new Set(semanticSelector.split(','));
          const semanticNodes = semanticElements.map((element, index) => {
            const tag = element.tagName.toLowerCase();
            const id = `semantic-${index}`;
            let depth = 0;
            for (let parent = element.parentElement; parent && parent !== document.body; parent = parent.parentElement) {
              if (semanticSet.has(parent.tagName.toLowerCase())) depth++;
            }
            let summary = labelledName(element);
            if (!summary && /^h[1-6]$/.test(tag)) summary = element.innerText || '';
            if (!summary && ['header','nav','main','footer','aside','section','article','form'].includes(tag))
              summary = element.querySelector('h1,h2,h3,h4,h5,h6')?.innerText || '';
            if (!summary && ['ul','ol','menu'].includes(tag))
              summary = `${element.querySelectorAll(':scope > li').length} éléments`;
            summary = summary.trim().replace(/\s+/g, ' ').slice(0, 160);
            let locator = element.id ? `#${CSS.escape(element.id)}` : tag;
            if (!element.id && element.parentElement) {
              const peers = [...element.parentElement.children].filter(sibling => sibling.tagName === element.tagName);
              locator += `:nth-of-type(${peers.indexOf(element) + 1})`;
            }
            const issue = issueByElement.get(element);
            return { id, badge: /^h[1-6]$/.test(tag) ? tag.toUpperCase() : `<${tag}>`, tag, summary, depth, locator,
              ruleId: issue?.ruleId || '', issue: issue?.message || '',
              issueLabel: issue ? (issue.advisory ? 'À vérifier' : 'Balise mal placée') : '', advisory: issue?.advisory || false };
          });
          const semanticNodeMap = new Map(semanticElements.map((element, index) => [`semantic-${index}`, element]));
          let previousSemantic = null;
          window.__rgaaStudioSemanticNodes = semanticNodeMap;
          window.__rgaaStudioHighlightSemanticNode = id => {
            if (previousSemantic) {
              previousSemantic.element.style.outline = previousSemantic.outline;
              previousSemantic.element.style.outlineOffset = previousSemantic.offset;
            }
            const element = semanticNodeMap.get(id);
            if (!element) return false;
            previousSemantic = { element, outline: element.style.outline, offset: element.style.outlineOffset };
            element.scrollIntoView({ block: 'center', behavior: 'smooth' });
            element.style.outline = '3px solid #D97706';
            element.style.outlineOffset = '2px';
            return true;
          };
          window.__rgaaStudioClearSemanticHighlight = () => {
            if (!previousSemantic) return;
            previousSemantic.element.style.outline = previousSemantic.outline;
            previousSemantic.element.style.outlineOffset = previousSemantic.offset;
            previousSemantic = null;
          };
          return JSON.stringify(semanticNodes);
        })()
        """;

    private const string BuildMediaTreeScript = """
        (() => {
          const selector = 'img,video,audio,svg:not([aria-hidden="true"]),canvas,object,embed';
          const visible = element => !element.hidden && element.getAttribute('aria-hidden') !== 'true' &&
            getComputedStyle(element).display !== 'none' && getComputedStyle(element).visibility !== 'hidden';
          const elements = [...document.querySelectorAll(selector)].filter(visible);
          const nodes = [];
          const nodeMap = new Map();
          const nameOf = element => {
            const labelledby = element.getAttribute('aria-labelledby');
            const referenced = labelledby ? labelledby.split(/\s+/).map(id => document.getElementById(id)?.innerText || '').join(' ').trim() : '';
            return (element.getAttribute('aria-label') || referenced || element.getAttribute('alt') ||
              element.getAttribute('title') || element.querySelector(':scope > title')?.textContent || '').trim();
          };
          const sourceOf = element => element.currentSrc || element.getAttribute('src') ||
            element.getAttribute('poster') || element.querySelector('source[src]')?.getAttribute('src') || '';
          const fileName = source => {
            if (!source) return '';
            try { return new URL(source, document.baseURI).pathname.split('/').filter(Boolean).pop() || source; }
            catch (_) { return source; }
          };
          const locatorOf = element => {
            if (element.id) return `#${CSS.escape(element.id)}`;
            const tag = element.tagName.toLowerCase();
            const peers = [...element.parentElement.children].filter(peer => peer.tagName === element.tagName);
            return `${tag}:nth-of-type(${peers.indexOf(element) + 1})`;
          };
          elements.forEach((element, index) => {
            const tag = element.tagName.toLowerCase();
            const id = `media-${index}`;
            const name = nameOf(element);
            const sourceName = fileName(sourceOf(element));
            let ruleId = '';
            let issue = '';
            let advisory = false;
            let issueLabel = '';
            if (tag === 'img' && !element.hasAttribute('alt') && element.getAttribute('aria-hidden') !== 'true' && element.getAttribute('role') !== 'presentation') {
              ruleId = `media.img.alt-missing.${index}`;
              issue = 'L’image ne possède pas d’attribut alt. Ajouter une alternative, ou alt="" si elle est décorative.';
              issueLabel = 'Alternative manquante';
            } else if (tag === 'img' && element.getAttribute('alt') === '' && element.closest('a,button') && !nameOf(element.closest('a,button'))) {
              ruleId = `media.img.link-name.${index}`;
              issue = 'Cette image décorative est le seul contenu d’un lien ou bouton sans nom accessible.';
              issueLabel = 'Lien sans nom';
            } else if (tag === 'video' && ![...element.querySelectorAll('track')].some(track => (track.kind || '').toLowerCase() === 'captions')) {
              ruleId = `media.video.captions.${index}`;
              issue = 'Vérifier la présence de sous-titres si la vidéo contient une piste audio.';
              issueLabel = 'Sous-titres à vérifier';
              advisory = true;
            } else if (tag === 'audio' && !/transcription|transcript/i.test(element.parentElement?.innerText || '')) {
              ruleId = `media.audio.transcript.${index}`;
              issue = 'Vérifier qu’une transcription textuelle de cet audio est disponible.';
              issueLabel = 'Transcription à vérifier';
              advisory = true;
            } else if (tag === 'svg' && !name && element.getAttribute('role') !== 'presentation') {
              ruleId = `media.svg.name.${index}`;
              issue = 'Le SVG visible n’a pas de nom accessible. Vérifier s’il est décoratif ou informatif.';
              issueLabel = 'Nom du SVG à vérifier';
              advisory = true;
            } else if (tag === 'canvas' && !name && !element.textContent.trim()) {
              ruleId = `media.canvas.fallback.${index}`;
              issue = 'Le canvas n’a pas de nom ni de contenu de remplacement accessible.';
              issueLabel = 'Contenu de remplacement à vérifier';
              advisory = true;
            } else if (['object','embed'].includes(tag) && !name) {
              ruleId = `media.${tag}.name.${index}`;
              issue = `L’élément <${tag}> n’a pas de nom accessible.`;
              issueLabel = 'Nom accessible à vérifier';
              advisory = true;
            }
            const semanticParents = new Set(['main','header','nav','footer','aside','section','article','figure']);
            let depth = 0;
            for (let parent = element.parentElement; parent && parent !== document.body; parent = parent.parentElement)
              if (semanticParents.has(parent.tagName.toLowerCase())) depth++;
            const summary = name || sourceName || (tag === 'img' && element.getAttribute('alt') === '' ? 'Image décorative' : 'Sans description');
            const node = {
              id,
              badge: `<${tag}>`,
              tag,
              summary: summary.trim().replace(/\s+/g, ' ').slice(0, 160),
              depth: Math.min(depth, 10),
              locator: locatorOf(element),
              ruleId,
              issue,
              issueLabel,
              advisory
            };
            nodes.push(node);
            nodeMap.set(id, element);
          });
          let previous = null;
          window.__rgaaStudioSemanticNodes = nodeMap;
          window.__rgaaStudioHighlightSemanticNode = id => {
            if (previous) { previous.element.style.outline = previous.outline; previous.element.style.outlineOffset = previous.offset; }
            const element = nodeMap.get(id);
            if (!element) return false;
            previous = { element, outline: element.style.outline, offset: element.style.outlineOffset };
            element.scrollIntoView({ block: 'center', behavior: 'smooth' });
            element.style.outline = '3px solid #D97706'; element.style.outlineOffset = '2px'; return true;
          };
          window.__rgaaStudioClearSemanticHighlight = () => {
            if (!previous) return;
            previous.element.style.outline = previous.outline; previous.element.style.outlineOffset = previous.offset; previous = null;
          };
          return JSON.stringify(nodes);
        })()
        """;

    private const string LegacyMediaTreeScript = """
        (() => {
          const selector = 'img,video,audio,svg:not([aria-hidden="true"]),canvas,object,embed';
          const visible = element => !element.hidden && element.getAttribute('aria-hidden') !== 'true' &&
            getComputedStyle(element).display !== 'none' && getComputedStyle(element).visibility !== 'hidden';
          const elements = [...document.querySelectorAll(selector)].filter(visible);
          const nodes = [];
          const nodeMap = new Map();
          const fileName = value => {
            if (!value) return '';
            try { return new URL(value, document.baseURI).pathname.split('/').filter(Boolean).pop() || value; }
            catch (_) { return value; }
          };
          const labelOf = element => element.getAttribute('aria-label') || element.getAttribute('title') ||
            element.querySelector(':scope > title')?.textContent || '';
          const locatorOf = element => {
            if (element.id) return `#${CSS.escape(element.id)}`;
            const tag = element.tagName.toLowerCase();
            const siblings = [...element.parentElement.children].filter(sibling => sibling.tagName === element.tagName);
            return `${tag}:nth-of-type(${siblings.indexOf(element) + 1})`;
          };
          elements.forEach((element, index) => {
            const tag = element.tagName.toLowerCase();
            const id = `media-${index}`;
            const label = labelOf(element).trim();
            const alt = tag === 'img' && element.hasAttribute('alt') ? element.getAttribute('alt').trim() : null;
            const source = tag === 'img' ? element.currentSrc || element.getAttribute('src') :
              tag === 'video' ? element.getAttribute('poster') || element.currentSrc || element.getAttribute('src') :
              tag === 'audio' ? element.currentSrc || element.getAttribute('src') :
              element.getAttribute('src') || element.getAttribute('href');
            let summary = label || (alt !== null ? (alt || 'Alternative vide — image décorative') : fileName(source));
            if (!summary) summary = 'Sans nom accessible';
            let ruleId = '';
            let issue = '';
            let advisory = false;
            if (tag === 'img' && alt === null && element.getAttribute('aria-hidden') !== 'true' && element.getAttribute('role') !== 'presentation') {
              ruleId = `media.img.alt-missing.${index}`;
              issue = 'Attribut alt absent. Ajouter une alternative textuelle, ou alt="" si l’image est décorative.';
            } else if (tag === 'img' && alt === '' && element.closest('a,button') && !labelOf(element.closest('a,button'))) {
              ruleId = `media.img.link-name.${index}`;
              issue = 'Cette image décorative se trouve dans un lien ou bouton sans autre nom accessible.';
            } else if (tag === 'video' && ![...element.querySelectorAll('track')].some(track => (track.kind || '').toLowerCase() === 'captions')) {
              ruleId = `media.video.captions.${index}`;
              issue = 'Vérifier la présence de sous-titres si la vidéo contient une piste audio.';
              advisory = true;
            } else if (tag === 'audio' && !/transcription|transcript/i.test(element.parentElement?.innerText || '')) {
              ruleId = `media.audio.transcript.${index}`;
              issue = 'Vérifier qu’une transcription textuelle de cet audio est disponible.';
              advisory = true;
            } else if (tag === 'svg' && !label && !element.getAttribute('aria-hidden') && !element.getAttribute('role')) {
              ruleId = `media.svg.name.${index}`;
              issue = 'SVG sans nom accessible ni rôle explicite. Vérifier s’il est décoratif ou informatif.';
              advisory = true;
            } else if (tag === 'canvas' && !element.getAttribute('aria-label') && !element.textContent.trim()) {
              ruleId = `media.canvas.fallback.${index}`;
              issue = 'Canvas sans alternative textuelle ou contenu de repli.';
              advisory = true;
            }
            const depth = (() => { let value = 0; for (let p = element.parentElement; p && p !== document.body; p = p.parentElement) value++; return Math.min(value, 10); })();
            const badge = tag.toUpperCase();
            nodes.push({ id, badge, tag, summary: summary.slice(0, 160), depth, locator: locatorOf(element), ruleId, issue, advisory });
            nodeMap.set(id, element);
          });
          let previous = null;
          window.__rgaaStudioSemanticNodes = nodeMap;
          window.__rgaaStudioHighlightSemanticNode = id => {
            if (previous) { previous.element.style.outline = previous.outline; previous.element.style.outlineOffset = previous.offset; }
            const element = nodeMap.get(id);
            if (!element) return false;
            previous = { element, outline: element.style.outline, offset: element.style.outlineOffset };
            element.scrollIntoView({ block: 'center', behavior: 'smooth' });
            element.style.outline = '3px solid #D97706'; element.style.outlineOffset = '2px'; return true;
          };
          window.__rgaaStudioClearSemanticHighlight = () => {
            if (!previous) return;
            previous.element.style.outline = previous.outline; previous.element.style.outlineOffset = previous.offset; previous = null;
          };
          return JSON.stringify(nodes);
        })()
        """;

    private const string BuildAriaTreeScript = """
        (() => {
          const root = document.body || document.documentElement;
          function labelledName(element) {
            const label = element.getAttribute('aria-label');
            if (label && label.trim()) return label.trim();
            const labelledby = element.getAttribute('aria-labelledby');
            if (labelledby) {
              const text = labelledby.split(/\s+/).map(id => document.getElementById(id)?.innerText || '').join(' ').trim();
              if (text) return text;
            }
            const title = element.getAttribute('title');
            return title && title.trim() ? title.trim() : '';
          }
          const roleOf = element => {
            const explicit = (element.getAttribute('role') || '').trim().split(/\s+/)[0];
            if (explicit) return explicit;
            const tag = element.tagName.toLowerCase();
            if (/^h[1-6]$/.test(tag)) return 'heading';
            if (tag === 'a' && element.hasAttribute('href')) return 'link';
            if (tag === 'button' || tag === 'summary') return 'button';
            if (tag === 'nav') return 'navigation';
            if (tag === 'main') return 'main';
            if (tag === 'header' && element.parentElement === document.body) return 'banner';
            if (tag === 'footer' && element.parentElement === document.body) return 'contentinfo';
            if (tag === 'aside') return 'complementary';
            if (tag === 'section') return labelledName(element) ? 'region' : 'generic';
            if (tag === 'form') return labelledName(element) ? 'form' : 'generic';
            if (tag === 'ul' || tag === 'ol' || tag === 'menu') return 'list';
            if (tag === 'li') return 'listitem';
            if (tag === 'img') return element.getAttribute('alt') === '' ? 'presentation' : 'img';
            if (tag === 'textarea') return 'textbox';
            if (tag === 'select') return element.multiple ? 'listbox' : 'combobox';
            if (tag === 'table') return 'table';
            if (tag === 'tr') return 'row';
            if (tag === 'th') return element.scope === 'row' ? 'rowheader' : 'columnheader';
            if (tag === 'td') return 'cell';
            if (tag === 'input') {
              const type = (element.type || 'text').toLowerCase();
              if (type === 'hidden') return 'presentation';
              if (type === 'checkbox') return 'checkbox';
              if (type === 'radio') return 'radio';
              if (type === 'button' || type === 'submit' || type === 'reset') return 'button';
              if (type === 'search') return 'searchbox';
              return 'textbox';
            }
            if (tag === 'progress') return 'progressbar';
            if (tag === 'meter') return 'meter';
            if (tag === 'dialog') return element.getAttribute('aria-modal') === 'true' ? 'dialog (modal)' : 'dialog (non-modal)';
            return 'generic';
          };
          function accessibleName(element, role) {
            const explicit = labelledName(element);
            if (explicit) return explicit;
            if (element.tagName.toLowerCase() === 'img' && element.hasAttribute('alt')) return element.getAttribute('alt').trim();
            if (element.id) {
              const label = document.querySelector(`label[for="${CSS.escape(element.id)}"]`);
              if (label?.innerText.trim()) return label.innerText.trim();
            }
            if (element.labels?.length) {
              const labels = [...element.labels].map(item => item.innerText).join(' ').trim();
              if (labels) return labels;
            }
            if (element.value && ['input','textarea'].includes(element.tagName.toLowerCase())) return element.value.trim();
            if (role === 'generic') return [...element.childNodes].filter(node => node.nodeType === Node.TEXT_NODE).map(node => node.textContent || '').join(' ').trim().replace(/\s+/g, ' ');
            if (['link','button','heading','listitem','option','menuitem','tab'].includes(role)) return (element.innerText || '').trim().replace(/\s+/g, ' ');
            return '';
          }
          const omitted = new Set(['presentation','none']);
          const hidden = element => element.hidden || element.getAttribute('aria-hidden') === 'true' ||
            element.getAttribute('type') === 'hidden' || getComputedStyle(element).display === 'none' ||
            getComputedStyle(element).visibility === 'hidden';
          function collect(element) {
            if (hidden(element)) return [];
            const role = roleOf(element);
            const name = accessibleName(element, role).slice(0, 180);
            const children = [...element.children].flatMap(child => collect(child));
            if (omitted.has(role) || role === 'generic' && !name && children.length === 0)
              return children;

            const nativeHeadingLevel = /^h[1-6]$/.test(element.tagName.toLowerCase()) ? element.tagName.slice(1) : '2';
            const badge = role === 'heading' ? `H${element.getAttribute('aria-level') || nativeHeadingLevel}` : role.toUpperCase();
            const itemName = role === 'list' && !name ? `${element.querySelectorAll(':scope > li').length} éléments` : name;
            let state = '';
            if (role === 'checkbox' || role === 'radio') state += element.checked ? ' · coché' : ' · non coché';
            if (element.disabled) state += ' · désactivé';
            if (element.required) state += ' · obligatoire';
            if (element.getAttribute('aria-expanded')) state += element.getAttribute('aria-expanded') === 'true' ? ' · développé' : ' · réduit';
            const requiresName = ['heading','link','button','img','textbox','searchbox','checkbox','radio','combobox','listbox','switch','slider','spinbutton'].includes(role);
            return [{ element, badge, role, name: itemName, missingName: requiresName && !itemName, isLink: role === 'link', state, children }];
          }
          const tree = [...root.children].flatMap(child => collect(child));
          const nodes = [];
          let previous = null;
          const nodeMap = new Map();
          function flatten(items, depth) {
            for (const item of items) {
              if (nodes.length >= 3000) return;
              const id = `aria-${nodes.length}`;
              nodes.push({ id, badge: item.badge, role: item.role, name: item.name, missingName: item.missingName, depth, isLink: item.isLink, state: item.state });
              nodeMap.set(id, item.element);
              flatten(item.children, depth + 1);
            }
          }
          flatten(tree, 0);
          window.__rgaaStudioAriaNodes = nodeMap;
          window.__rgaaStudioHighlightAriaNode = id => {
            if (previous) {
              previous.element.style.outline = previous.outline;
              previous.element.style.outlineOffset = previous.offset;
            }
            const element = nodeMap.get(id);
            if (!element) return false;
            previous = { element, outline: element.style.outline, offset: element.style.outlineOffset };
            element.scrollIntoView({ block: 'center', behavior: 'smooth' });
            element.style.outline = '3px solid #D97706';
            element.style.outlineOffset = '2px';
            return true;
          };
          window.__rgaaStudioClearAriaHighlight = () => {
            if (!previous) return;
            previous.element.style.outline = previous.outline;
            previous.element.style.outlineOffset = previous.offset;
            previous = null;
          };
          return JSON.stringify(nodes);
        })()
        """;

    private readonly EmbeddedAuditDatabase _dossiers = new();
    private readonly SiteDiscoveryService _siteDiscovery = new();
    private DesktopUserPreferences _userPreferences = new();
    private AuditManifest? _activeAudit;
    private string? _activeAuditPageId;
    private string? _pageSourceSnapshot;
    private string _pageViewMode = "site";
    private bool _siteStylesRemoved;
    private TaskCompletionSource<bool>? _browserRefreshCompletion;
    private byte[]? _pendingFavicon;
    private bool _addingPagesToExistingAudit;
    private string? _existingAuditIdForPageSelection;
    private bool _showArchivedAudits;
    private bool _inspectionActive;
    private bool _windowIsActive = true;
    private bool _sidePanelShown = true;
    private bool _pageScopeCollapsed;
    private bool _tabLayoutUpdateScheduled;
    private StoredAuditSuggestion? _selectedAxeSuggestion;
    private string _selectedAxeTarget = string.Empty;
    private string _selectedAxeTag = string.Empty;
    private string _selectedAxeText = string.Empty;
    private string _selectedAxeHtml = string.Empty;
    private AxeElementContext? _selectedAxeContext;
    private bool _contrastPipetteActive;
    private CancellationTokenSource? _contrastPickPolling;
    private ContrastReviewContext? _contrastReviewContext;
    private bool CanInspect => WorkspaceView.IsVisible && IsLocalFixture && _pageViewMode == "site";
    private bool IsLocalFixture => Browser.Source is { IsFile: true } source &&
        string.Equals(Path.GetFullPath(source.LocalPath), Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Assets", "fixture.html")), StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<AuditListItem> Audits { get; } = [];
    public ObservableCollection<OpenAuditTab> OpenAuditTabs { get; } = [];
    public ObservableCollection<string> ReferenceOptions { get; } = [];
    public ObservableCollection<string> VersionOptions { get; } = [];
    public ObservableCollection<string> LevelOptions { get; } = ["A", "AA", "AAA"];
    public ObservableCollection<SitePageChoice> PriorityPageChoices { get; } = [];
    public ObservableCollection<SitePageChoice> OtherPageChoices { get; } = [];
    public ObservableCollection<AuditPageListItem> AuditPages { get; } = [];
    public ObservableCollection<AriaTreeNode> AriaTreeNodes { get; } = [];
    public ObservableCollection<SemanticTreeNode> SemanticTreeNodes { get; } = [];
    public ObservableCollection<AxeResultItem> AxeResults { get; } = [];
    public ObservableCollection<InspectionEvidenceItem> InspectionEvidence { get; } = [];
    public ObservableCollection<TechnologyFinding> TechnologyFindings { get; } = [];
    public ObservableCollection<SeoFinding> SeoFindings { get; } = [];
    public ObservableCollection<AuditDashboardMetric> DashboardMetrics { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        _userPreferences = LoadUserPreferences();
        _sidePanelShown = _userPreferences.ShowInspectionPanel;
        _pageScopeCollapsed = _userPreferences.CollapsePageScope;
        DataContext = this;
        using (var highlightingStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RgaaStudio.Desktop.Highlighting.HtmlSource.xshd")
            ?? throw new InvalidOperationException("HTML syntax definition is missing."))
        using (var highlightingReader = XmlReader.Create(highlightingStream))
            SourceCodeView.SyntaxHighlighting = HighlightingLoader.Load(highlightingReader, HighlightingManager.Instance);
        OptionsPopup.PlacementTarget = OptionsButton;
        OpenAuditTabs.CollectionChanged += (_, _) => ScheduleTabLayoutUpdate();
        UpdateInspectionMenuCommands();
        UpdateProjectListState();
        Activated += (_, _) => UpdateWindowActivation(true);
        Deactivated += (_, _) =>
        {
            OptionsPopup.IsOpen = false;
            UpdateWindowActivation(false);
        };
        Loaded += (_, _) =>
        {
            UpdateHomeLayout();
            RefreshReferenceOptions();
            RefreshAudits();
        };
        Browser.NavigationStarted += Browser_NavigationStarted;
        Browser.NavigationCompleted += Browser_NavigationCompleted;
        Browser.WebMessageReceived += Browser_WebMessageReceived;
        CaptureAxeProofButton.IsEnabled = OperatingSystem.IsWindows();
        PipetteButton.Tag = false;
    }

    private async void RunAxeScan_Click(object? sender, RoutedEventArgs e) => await RunAxeScanAsync();

    private async Task RunAxeScanAsync()
    {
        if (_pageViewMode == "source")
            SetPageView("site");
        SetPageView("axe");
        BrowserModeLabel.Text = "Résultats axe-core";
        BrowserSafetyNote.Text = "Les résultats automatisés sont des aides à l’examen ; ils ne constituent pas une décision de conformité.";
        if (_activeAudit is null || _activeAuditPageId is null || Browser.Source is not { IsAbsoluteUri: true } source ||
            !Uri.TryCreate(source.AbsoluteUri, UriKind.Absolute, out var current) || current.Scheme != Uri.UriSchemeHttps ||
            !_dossiers.GetPages(_activeAudit.Id).Any(page => page.Id == _activeAuditPageId && Uri.Compare(new Uri(page.Url), current, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0))
        {
            AxeScanStatus.Text = "Ouvrez une page HTTPS incluse dans cet audit avant de lancer l’analyse.";
            return;
        }

        RunAxeScanButton.IsEnabled = false;
        AxeScanStatus.Text = "Analyse en cours…";
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RgaaStudio.Desktop.Assets.axe.min.js")
                ?? throw new InvalidOperationException("Le moteur axe-core embarqué est indisponible.");
            using var reader = new StreamReader(stream);
            var axeSource = await reader.ReadToEndAsync();
            await Browser.InvokeScript(axeSource);
            await Browser.InvokeScript("window.__rgaaStudioAxeResult=null;window.axe.run().then(r=>window.__rgaaStudioAxeResult=JSON.stringify({version:axe.version,violations:r.violations,incomplete:r.incomplete,inapplicable:r.inapplicable})).catch(e=>window.__rgaaStudioAxeResult=JSON.stringify({error:String(e)}));'started'");
            string? json = null;
            for (var attempt = 0; attempt < 120; attempt++)
            {
                await Task.Delay(250);
                json = DecodeScriptString(await Browser.InvokeScript("window.__rgaaStudioAxeResult || ''"));
                if (!string.IsNullOrEmpty(json)) break;
            }
            if (string.IsNullOrWhiteSpace(json)) throw new TimeoutException();
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 24 });
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error)) throw new InvalidOperationException(error.GetString());
            var version = root.GetProperty("version").GetString() ?? "unknown";
            var suggestions = new List<StoredAuditSuggestion>();
            AddAxeResults(root, "violations", "Violation", version, suggestions);
            AddAxeResults(root, "incomplete", "À vérifier", version, suggestions);
            _dossiers.SaveAxeSuggestions(_activeAudit.Id, _activeAuditPageId, suggestions);
            RefreshAxeResults();
            RefreshAuditDashboard();
            AxeScanStatus.Text = $"Analyse axe-core {version} terminée : {suggestions.Count} règles applicables avec des éléments à examiner. Les passes ne sont pas des décisions de conformité.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or JsonException or TimeoutException or ArgumentException)
        {
            AxeScanStatus.Text = "L’analyse n’a pas abouti. Vérifiez que la page est toujours chargée, puis réessayez.";
        }
        finally { RunAxeScanButton.IsEnabled = true; }
    }

    private async void PipetteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_contrastPipetteActive)
        {
            await StopContrastPipetteAsync("Pipette arrêtée.");
            return;
        }
        if (_siteStylesRemoved || _pageViewMode == "source" || !Browser.IsVisible)
        {
            PipetteStatus.Text = "Affichez d’abord la page du site, sans CSS désactivée, pour relever ses couleurs.";
            ColorContrastPanel.IsVisible = true;
            return;
        }
        if (Browser.Source is not { IsAbsoluteUri: true } source ||
            !(IsLocalFixture || IsSelectedAuditPageUrl(source)))
        {
            PipetteStatus.Text = "Sélectionnez une page incluse dans l’audit avant d’utiliser la pipette.";
            ColorContrastPanel.IsVisible = true;
            return;
        }

        try
        {
            _contrastPipetteActive = true;
            PipetteButton.Tag = true;
            PipetteButtonLabel.Text = "Annuler";
            PipetteStatus.Text = "Déplacez le pointeur sur le site, puis cliquez sur l’élément à mesurer. Échap pour annuler.";
            ColorContrastPanel.IsVisible = true;
            SelectedElementHeading.Text = "Pipette active";
            SelectionSummary.IsVisible = false;
            ContrastElementSummary.Text = "Pointez un texte ou un composant dans la page.";
            ToolTip.SetTip(ContrastElementSummary, null);
            ContrastRatioText.Text = string.Empty;
            ContrastAssessment.Text = string.Empty;
            ContrastCaveat.Text = string.Empty;
            ContrastCaveat.IsVisible = false;
            ContrastForegroundHex.Text = string.Empty;
            ContrastBackgroundHex.Text = string.Empty;
            ContrastForegroundSwatch.Background = Brush.Parse("#171F3B");
            ContrastBackgroundSwatch.Background = Brush.Parse("#FFFFFF");
            AddContrastFindingButton.IsVisible = false;
            _contrastReviewContext = null;
            await Browser.InvokeScript(StartContrastPipetteScript);
            _contrastPickPolling = new CancellationTokenSource();
            _ = PollContrastPickAsync(_contrastPickPolling.Token);
            Browser.Focus();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            await StopContrastPipetteAsync("La pipette n’a pas pu être activée sur cette page.");
        }
    }

    private async Task StopContrastPipetteAsync(string status)
    {
        _contrastPipetteActive = false;
        _contrastPickPolling?.Cancel();
        _contrastPickPolling?.Dispose();
        _contrastPickPolling = null;
        PipetteButton.Tag = false;
        PipetteButtonLabel.Text = "Pipette";
        PipetteStatus.Text = status;
        if (_contrastReviewContext is null)
        {
            ColorContrastPanel.IsVisible = false;
            SelectedElementHeading.Text = "Élément sélectionné";
            SelectionSummary.Text = status;
            SelectionSummary.IsVisible = true;
        }
        try { await Browser.InvokeScript("window.__rgaaContrastPipetteStop?.()"); }
        catch (Exception) { }
    }

    private async Task PollContrastPickAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_contrastPipetteActive && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(250, cancellationToken);
                var json = DecodeScriptString(await Browser.InvokeScript("window.__rgaaContrastPipettePicked || ''"));
                if (string.IsNullOrWhiteSpace(json)) continue;
                var sample = JsonSerializer.Deserialize<ContrastPipetteSample>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (sample is not null) await CompleteContrastPickAsync(sample);
                return;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) when (exception is InvalidOperationException or JsonException or ArgumentException)
        {
            if (_contrastPipetteActive)
                await StopContrastPipetteAsync("La pipette a été interrompue. Réessayez sur la page affichée.");
        }
    }

    private async Task CompleteContrastPickAsync(ContrastPipetteSample sample)
    {
        if (!_contrastPipetteActive) return;
        _contrastPipetteActive = false;
        _contrastPickPolling?.Cancel();
        _contrastPickPolling?.Dispose();
        _contrastPickPolling = null;
        PipetteButton.Tag = false;
        PipetteButtonLabel.Text = "Pipette";
        try { await Browser.InvokeScript("window.__rgaaContrastPipetteStop?.()"); }
        catch (Exception) { }

        var isText = !string.IsNullOrWhiteSpace(sample.Text);
        ColorContrastPanel.IsVisible = true;
        var fontSize = double.TryParse(sample.FontSize.Replace("px", "", StringComparison.OrdinalIgnoreCase),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedSize) ? parsedSize : 0;
        var fontWeight = sample.FontWeight.Equals("bold", StringComparison.OrdinalIgnoreCase) ? 700 :
            int.TryParse(sample.FontWeight, out var parsedWeight) ? parsedWeight : 400;
        var largeText = isText && (fontSize >= 24 || fontWeight >= 700 && fontSize >= 18.66);
        var isAaa = string.Equals(_activeAudit?.Level, "AAA", StringComparison.OrdinalIgnoreCase);
        var isLevelA = string.Equals(_activeAudit?.Level, "A", StringComparison.OrdinalIgnoreCase) &&
            _activeAudit?.Reference.Contains("WCAG", StringComparison.OrdinalIgnoreCase) == true;
        var foregroundColor = isText || !sample.HasBorder ? sample.Foreground : sample.BorderColor;
        var measurement = ColorContrastAnalyzer.Measure(foregroundColor, sample.BackgroundLayers ?? []);
        var isGraphicalObject = !isText && sample.HasBorder;
        var requiredRatio = isGraphicalObject ? 3 : ColorContrastAnalyzer.RequiredRatio(largeText, isAaa && !isGraphicalObject);
        var complexBackground = !string.IsNullOrWhiteSpace(sample.BackgroundImages) || !string.IsNullOrWhiteSpace(sample.OpacityLayers);
        var uncertainBackground = complexBackground || measurement is null;
        var belowThreshold = measurement is not null && measurement.Ratio < requiredRatio;
        var levelLabel = isGraphicalObject ? "repère WCAG AA graphique"
            : _activeAudit?.Reference.Contains("RGAA", StringComparison.OrdinalIgnoreCase) == true
                ? "repère RGAA / WCAG AA"
                : isAaa ? "seuil WCAG AAA" : isLevelA ? "repère complémentaire WCAG AA" : "seuil WCAG AA";

        SelectedElementHeading.Text = "Zone pointée";
        SelectionSummary.IsVisible = false;
        ContrastElementSummary.Text = $"<{sample.Tag}> · {(string.IsNullOrWhiteSpace(sample.Text) ? "aucun texte visible" : sample.Text)}";
        ToolTip.SetTip(ContrastElementSummary, sample.Selector);
        var shownForeground = measurement?.ForegroundHex ?? foregroundColor;
        var shownBackground = measurement?.BackgroundHex ?? sample.BackgroundLayers?.LastOrDefault() ?? "indéterminé";
        ContrastForegroundSwatch.Background = TryCreateColorBrush(shownForeground, "#171F3B");
        ContrastBackgroundSwatch.Background = TryCreateColorBrush(shownBackground, "#FFFFFF");
        ContrastForegroundHex.Text = shownForeground;
        ContrastBackgroundHex.Text = shownBackground;
        ContrastRatioText.Text = measurement is null ? "Ratio indisponible" : $"Ratio mesuré : {measurement.Ratio:F2}:1";

        var noCriterion = !isText && !sample.HasBorder;
        UpdateContrastLevelIndicators(measurement, noCriterion, largeText, isGraphicalObject, complexBackground);
        ContrastAssessment.Text = noCriterion
            ? "Aucun texte ou contour à évaluer."
            : measurement is null
                ? "Couleurs non interprétées : mesure à vérifier manuellement."
                : isLevelA && belowThreshold
                    ? "Sous le repère AA ; le dossier cible A, qui n’impose pas ce seuil."
                    : belowThreshold && complexBackground
                        ? "Sous le seuil calculé : écart potentiel à confirmer visuellement."
                        : belowThreshold
                            ? "Seuil manqué : écart à inspecter."
                            : complexBackground
                                ? "Seuil calculé atteint ; confirmer le fond complexe visuellement."
                                : "Seuils applicables atteints pour les couleurs calculées.";
        ContrastAssessment.Foreground = !noCriterion && belowThreshold && !isLevelA ? Brush.Parse("#8B2D24") : Brush.Parse("#45536D");
        ContrastCaveat.Text = complexBackground
            ? "Fond avec image, dégradé ou opacité : le ratio CSS ne couvre pas chaque pixel."
            : measurement is null
                ? "La couleur CSS renvoyée par le navigateur n’est pas reconnue par le calculateur local."
                : string.Empty;
        ContrastCaveat.IsVisible = complexBackground || measurement is null;
        AddContrastFindingButton.IsVisible = !noCriterion && (belowThreshold || uncertainBackground) && !isLevelA;
        _contrastReviewContext = new ContrastReviewContext(sample, measurement, requiredRatio, isText, isGraphicalObject,
            uncertainBackground);
        PipetteStatus.Text = string.Empty;
    }

    private void UpdateContrastLevelIndicators(ColorContrastMeasurement? measurement, bool noCriterion, bool largeText,
        bool graphicalObject, bool complexBackground)
    {
        SetContrastLevelBadge(ContrastLevelASurface, ContrastLevelAStatus, ContrastLevelAThreshold,
            "Non exigé", "Aucun seuil de contraste à A", "neutral");
        if (noCriterion)
        {
            SetContrastLevelBadge(ContrastLevelAASurface, ContrastLevelAAStatus, ContrastLevelAAThreshold,
                "Non classé", "Texte ou composant non identifié", "neutral");
            SetContrastLevelBadge(ContrastLevelAAASurface, ContrastLevelAAAStatus, ContrastLevelAAAThreshold,
                "Non classé", "Texte ou composant non identifié", "neutral");
            return;
        }
        if (measurement is null)
        {
            SetContrastLevelBadge(ContrastLevelAASurface, ContrastLevelAAStatus, ContrastLevelAAThreshold,
                "À mesurer", "Calcul CSS indisponible", "uncertain");
            SetContrastLevelBadge(ContrastLevelAAASurface, ContrastLevelAAAStatus, ContrastLevelAAAThreshold,
                "À mesurer", "Calcul CSS indisponible", "uncertain");
            return;
        }

        var aaThreshold = graphicalObject ? 3 : ColorContrastAnalyzer.RequiredRatio(largeText, aaa: false);
        var aaaThreshold = graphicalObject ? 3 : ColorContrastAnalyzer.RequiredRatio(largeText, aaa: true);
        SetContrastBadgeForLevel(ContrastLevelAASurface, ContrastLevelAAStatus, ContrastLevelAAThreshold,
            measurement.Ratio, aaThreshold, complexBackground);
        SetContrastBadgeForLevel(ContrastLevelAAASurface, ContrastLevelAAAStatus, ContrastLevelAAAThreshold,
            measurement.Ratio, aaaThreshold, complexBackground);
    }

    private static void SetContrastBadgeForLevel(Border surface, TextBlock state, TextBlock threshold,
        double ratio, double minimum, bool uncertain)
    {
        var label = uncertain ? "À confirmer" : ratio < minimum ? "À inspecter" : "Conforme";
        var category = uncertain ? "uncertain" : ratio < minimum ? "fail" : "pass";
        SetContrastLevelBadge(surface, state, threshold, label, $"Minimum {minimum:F1}:1", category);
    }

    private static void SetContrastLevelBadge(Border surface, TextBlock state, TextBlock threshold,
        string label, string detail, string category)
    {
        var (background, foreground) = category switch
        {
            "pass" => ("#E6F6F2", "#175F59"),
            "fail" => ("#FBEDEA", "#8B2D24"),
            "uncertain" => ("#FFF4D9", "#6A5016"),
            _ => ("#F3F4F6", "#555E70")
        };
        surface.Background = Brush.Parse(background);
        state.Text = label;
        state.Foreground = Brush.Parse(foreground);
        threshold.Text = detail;
    }

    private static IBrush TryCreateColorBrush(string value, string fallback)
    {
        try { return Brush.Parse(value); }
        catch (Exception exception) when (exception is FormatException or ArgumentException) { return Brush.Parse(fallback); }
    }

    private void AddContrastFinding_Click(object? sender, RoutedEventArgs e)
    {
        if (_activeAudit is null || _activeAuditPageId is null || _contrastReviewContext is not { } context)
        {
            PipetteStatus.Text = "Ouvrez une page de l’audit avant d’ajouter un constat.";
            return;
        }
        var sample = context.Sample;
        var selectorHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sample.Selector))).ToLowerInvariant()[..16];
        var sourceRule = "contrast.manual." + selectorHash;
        var referenceCriterion = _activeAudit.Reference.Contains("RGAA", StringComparison.OrdinalIgnoreCase)
            ? "RGAA 3.2 · contraste des textes et composants"
            : context.IsText ? "WCAG 1.4.3 · contraste minimum" : "WCAG 1.4.11 · contraste des composants";
        var ratio = context.Measurement?.Ratio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) ?? "non calculable";
        var foreground = context.Measurement?.ForegroundHex ?? sample.Foreground;
        var background = context.Measurement?.BackgroundHex ?? string.Join(" sur ", sample.BackgroundLayers ?? []);
        var description = $"Pipette de contraste : {ratio}:1, comparaison au seuil {context.RequiredRatio:F1}:1. Couleur relevée {foreground} sur {background}. Élément : {(string.IsNullOrWhiteSpace(sample.Text) ? "sans texte visible" : sample.Text)}. Ce relevé est un indice à confirmer manuellement dans l’état concerné.";
        try
        {
            var added = _dossiers.AddSemanticFindingToAudit(_activeAudit.Id, _activeAuditPageId, sourceRule,
                "Contraste de couleur à inspecter", description, sample.Selector, "Pipette de contraste");
            RuleBox.Text = referenceCriterion;
            PipetteStatus.Text = added ? "Constat de contraste ajouté au dossier pour examen." : "Un constat de contraste existe déjà pour cet élément.";
            _activeAudit = _dossiers.Get(_activeAudit.Id) ?? _activeAudit;
            RefreshAudits();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            PipetteStatus.Text = exception.Message;
        }
    }

    private static void AddAxeResults(JsonElement root, string property, string outcome, string version, List<StoredAuditSuggestion> output)
    {
        if (!root.TryGetProperty(property, out var rules) || rules.ValueKind != JsonValueKind.Array) return;
        foreach (var rule in rules.EnumerateArray())
        {
            var ruleId = rule.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
            var help = rule.TryGetProperty("help", out var helpElement) ? helpElement.GetString() ?? ruleId : ruleId;
            var impact = rule.TryGetProperty("impact", out var impactElement) ? impactElement.GetString() ?? "" : "";
            var nodes = rule.TryGetProperty("nodes", out var nodeArray) ? nodeArray : default;
            var evidence = nodes.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Serialize(nodes.EnumerateArray().Take(100).Select(node => new
                {
                    target = node.TryGetProperty("target", out var target)
                        ? target.ValueKind == JsonValueKind.Array
                            ? target.EnumerateArray().Select(part => part.GetString()).FirstOrDefault(part => !string.IsNullOrWhiteSpace(part)) ?? ""
                            : target.ToString()
                        : "",
                    html = node.TryGetProperty("html", out var html) ? html.ToString() : "",
                    summary = node.TryGetProperty("failureSummary", out var summary) ? summary.ToString() : ""
                }))
                : "[]";
            var count = nodes.ValueKind == JsonValueKind.Array ? nodes.GetArrayLength() : 0;
            if (count == 0) continue;
            output.Add(new StoredAuditSuggestion
            {
                RuleId = ruleId,
                ReferenceTestId = ruleId,
                Outcome = outcome,
                Summary = $"{help} · {count} élément(s)",
                Impact = impact,
                EvidenceJson = evidence,
                AnalyzerVersion = version
            });
        }
    }

    private void RefreshAxeResults()
    {
        AxeResults.Clear();
        if (_activeAudit is null || _activeAuditPageId is null) return;
        var page = _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(item => item.Id == _activeAuditPageId);
        if (page is null) return;
        foreach (var suggestion in page.Suggestions.Where(item => item.AnalyzerVersion.StartsWith("axe-core ", StringComparison.Ordinal) && item.Outcome is "Violation" or "À vérifier").OrderBy(item => item.Outcome))
            AxeResults.Add(new AxeResultItem(suggestion.Id, $"{suggestion.Outcome} · {suggestion.RuleId}", suggestion.Summary, suggestion.ReviewStatus));
        NoAxeResults.IsVisible = AxeResults.Count == 0;
    }

    private async void AxeResult_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not string id || _activeAudit is null || _activeAuditPageId is null) return;
        var item = _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(page => page.Id == _activeAuditPageId)?.Suggestions.FirstOrDefault(result => result.Id == id);
        if (item is null) return;
        await PopulateAxeInspectionAsync(item);
    }

    private void AxeKeep_Click(object? sender, RoutedEventArgs e) => ReviewAxeResult(sender, "Retenu");
    private void AxeReject_Click(object? sender, RoutedEventArgs e) => ReviewAxeResult(sender, "Écarté");
    private async void ReviewAxeResult(object? sender, string status)
    {
        if ((sender as Button)?.Tag is not string id || _activeAudit is null || _activeAuditPageId is null) return;
        var suggestions = _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(page => page.Id == _activeAuditPageId)?.Suggestions;
        var suggestion = suggestions?.FirstOrDefault(item => item.Id == id);
        if (suggestion is null) return;
        _dossiers.SetAxeSuggestionReview(_activeAudit.Id, _activeAuditPageId, id, status);
        if (status == "Retenu" && suggestion.Outcome == "Violation")
        {
            var locator = string.Empty;
            try
            {
                using var evidence = JsonDocument.Parse(suggestion.EvidenceJson);
                var node = evidence.RootElement.EnumerateArray().FirstOrDefault();
                if (node.TryGetProperty("target", out var target)) locator = target.GetString() ?? string.Empty;
            }
            catch (JsonException) { }
            _dossiers.AddSemanticFindingToAudit(_activeAudit.Id, _activeAuditPageId, "axe." + suggestion.RuleId,
                "Vérifier : " + suggestion.Summary, suggestion.Summary + " (analyse automatique axe-core ; à confirmer par l’auditeur).",
                locator, "Analyse axe-core");
        }
        if (status == "Retenu")
        {
            RuleBox.Text = suggestion.RuleId;
            await PopulateAxeInspectionAsync(suggestion);
        }
        RefreshAxeResults();
        RefreshAuditDashboard();
        AxeScanStatus.Text = status == "Retenu"
            ? suggestion.Outcome == "Violation"
                ? "Résultat retenu ; un constat à confirmer a été ajouté au dossier."
                : "Suggestion retenue dans la revue axe-core ; vérifiez-la avec le référentiel et le contexte de la page."
            : "Résultat écarté par l’auditeur.";
    }

    private async Task PopulateAxeInspectionAsync(StoredAuditSuggestion suggestion)
    {
        try
        {
            ColorContrastPanel.IsVisible = false;
            SelectedElementHeading.Text = "Élément sélectionné";
            SelectionSummary.IsVisible = true;
            using var evidence = JsonDocument.Parse(suggestion.EvidenceJson);
            var firstNode = evidence.RootElement.ValueKind == JsonValueKind.Array
                ? evidence.RootElement.EnumerateArray().FirstOrDefault()
                : default;
            var target = firstNode.ValueKind == JsonValueKind.Object && firstNode.TryGetProperty("target", out var targetElement)
                ? targetElement.GetString() ?? string.Empty : string.Empty;
            var fallbackHtml = firstNode.ValueKind == JsonValueKind.Object && firstNode.TryGetProperty("html", out var htmlElement)
                ? htmlElement.GetString() ?? string.Empty : string.Empty;
            _selectedAxeSuggestion = suggestion;
            _selectedAxeTarget = target;
            _selectedAxeTag = string.Empty;
            _selectedAxeText = string.Empty;
            _selectedAxeContext = null;
            AxeTechnologyInfo.Text = "Analyse locale du DOM en cours…";
            var encodedTarget = JsonSerializer.Serialize(target);
            var script = ReadAxeElementScript.Replace("__TARGET__", encodedTarget, StringComparison.Ordinal);
            var json = DecodeScriptString(await Browser.InvokeScript(script));
            if (!string.IsNullOrWhiteSpace(json))
            {
                _selectedAxeContext = JsonSerializer.Deserialize<AxeElementContext>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_selectedAxeContext?.Missing != true)
                {
                    _selectedAxeTag = _selectedAxeContext?.Tag ?? string.Empty;
                    _selectedAxeText = _selectedAxeContext?.Text ?? string.Empty;
                    _selectedAxeHtml = _selectedAxeContext?.Html ?? fallbackHtml;
                    AxeTechnologyInfo.Text = _selectedAxeContext?.Technologies is { Count: > 0 } technologies
                        ? string.Join(" · ", technologies) + "\nDétection indicative depuis le DOM et les globals visibles ; aucune sonde réseau."
                        : "Aucune technologie identifiable dans les indices DOM visibles. Cela ne prouve pas son absence.";
                }
            }
            if (string.IsNullOrWhiteSpace(_selectedAxeHtml)) _selectedAxeHtml = fallbackHtml;
            AxeHtmlEvidence.Text = _selectedAxeHtml;
            AxeElementSummary.Text = $"{suggestion.RuleId} · {suggestion.Outcome}\n{suggestion.Summary}\nSélecteur : {target}";
            SelectionSummary.Text = $"Résultat axe-core retenu pour examen\nRègle : {suggestion.RuleId}\nÉlément : {target}";
            AxeEvidencePanel.IsVisible = true;
            RefreshInspectionEvidence();
            if (!string.IsNullOrWhiteSpace(target))
            {
                var encoded = JsonSerializer.Serialize(target);
                await Browser.InvokeScript($"(()=>{{document.querySelectorAll('[data-rgaa-axe-highlight]').forEach(e=>{{e.style.outline=e.dataset.rgaaOldOutline||'';delete e.dataset.rgaaAxeHighlight;delete e.dataset.rgaaOldOutline}});let e;try{{e=document.querySelector({encoded})}}catch{{}}if(!e)return false;e.dataset.rgaaAxeHighlight='1';e.dataset.rgaaOldOutline=e.style.outline;e.style.outline='3px solid #D97706';e.scrollIntoView({{block:'center',behavior:'smooth'}});return true}})()");
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
        {
            AxeProofStatus.Text = "Les détails DOM n’ont pas pu être relus ; l’extrait axe-core reste disponible si présent.";
            _selectedAxeSuggestion = suggestion;
            try
            {
                using var fallback = JsonDocument.Parse(suggestion.EvidenceJson);
                var node = fallback.RootElement.EnumerateArray().FirstOrDefault();
                _selectedAxeHtml = node.TryGetProperty("html", out var html) ? html.GetString() ?? string.Empty : string.Empty;
                _selectedAxeTarget = node.TryGetProperty("target", out var selector) ? selector.GetString() ?? string.Empty : string.Empty;
                AxeHtmlEvidence.Text = _selectedAxeHtml;
                AxeElementSummary.Text = $"{suggestion.RuleId} · {suggestion.Summary}\nSélecteur : {_selectedAxeTarget}";
                AxeEvidencePanel.IsVisible = true;
            }
            catch (JsonException) { }
        }
    }

    private void AddHtmlProof_Click(object? sender, RoutedEventArgs e)
    {
        if (_activeAudit is null || _activeAuditPageId is null || _selectedAxeSuggestion is null || string.IsNullOrWhiteSpace(_selectedAxeHtml)) return;
        _dossiers.AddEvidence(_activeAudit.Id, _activeAuditPageId, "axe-core · " + _selectedAxeSuggestion.RuleId,
            _selectedAxeTag, _selectedAxeText, "Extrait DOM HTML relu depuis le WebView",
            _selectedAxeHtml);
        AxeProofStatus.Text = "Extrait HTML enregistré comme preuve locale.";
        RefreshInspectionEvidence();
    }

    private async void CaptureAxeProof_Click(object? sender, RoutedEventArgs e)
    {
        if (!OperatingSystem.IsWindows())
        {
            AxeProofStatus.Text = "La capture de zone est actuellement disponible sous Windows uniquement.";
            return;
        }
        if (_activeAudit is null || _activeAuditPageId is null || _selectedAxeSuggestion is null || string.IsNullOrWhiteSpace(_selectedAxeTarget)) return;
        try
        {
            var encoded = JsonSerializer.Serialize(_selectedAxeTarget);
            var json = DecodeScriptString(await Browser.InvokeScript($"(()=>{{let e;try{{e=document.querySelector({encoded})}}catch{{}}if(!e)return '';e.scrollIntoView({{block:'center',inline:'center'}});const r=e.getBoundingClientRect();return JSON.stringify({{x:r.x,y:r.y,width:r.width,height:r.height,viewportWidth:innerWidth,viewportHeight:innerHeight}})}})()"));
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("L’élément n’est plus présent dans la page.");
            var rect = JsonSerializer.Deserialize<AxeElementRect>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (rect is null || rect.Width <= 0 || rect.Height <= 0 || rect.ViewportWidth <= 0) throw new InvalidOperationException("La zone sélectionnée n’est pas visible.");
            await Task.Delay(250);
            var scaling = TopLevel.GetTopLevel(Browser)?.RenderScaling ?? 1;
            var origin = Browser.PointToScreen(new Point(0, 0));
            var browserWidth = Math.Max(1, (int)Math.Round(Browser.Bounds.Width * scaling));
            var browserHeight = Math.Max(1, (int)Math.Round(Browser.Bounds.Height * scaling));
            var scaleX = browserWidth / rect.ViewportWidth;
            var scaleY = browserHeight / rect.ViewportHeight;
            var left = (int)Math.Floor(rect.X * scaleX);
            var top = (int)Math.Floor(rect.Y * scaleY);
            var right = (int)Math.Ceiling((rect.X + rect.Width) * scaleX);
            var bottom = (int)Math.Ceiling((rect.Y + rect.Height) * scaleY);
            left = Math.Clamp(left, 0, browserWidth - 1); top = Math.Clamp(top, 0, browserHeight - 1);
            right = Math.Clamp(right, left + 1, browserWidth); bottom = Math.Clamp(bottom, top + 1, browserHeight);
            var png = WindowsScreenCapture.CapturePng(origin.X + left, origin.Y + top, right - left, bottom - top);
            _dossiers.AddEvidence(_activeAudit.Id, _activeAuditPageId, "axe-core · " + _selectedAxeSuggestion.RuleId,
                _selectedAxeTag, _selectedAxeText, "Capture d’écran de la zone visible du WebView",
                $"Élément : {_selectedAxeTarget}\nCapture limitée à la zone actuellement visible de l’élément.", png, "image/png");
            AxeProofStatus.Text = "Capture de l’élément enregistrée comme preuve locale.";
            RefreshInspectionEvidence();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or ExternalException or IOException)
        {
            AxeProofStatus.Text = "Capture impossible : " + exception.Message;
        }
    }

    private void RefreshInspectionEvidence()
    {
        InspectionEvidence.Clear();
        if (_activeAudit is null || _activeAuditPageId is null)
        {
            NoInspectionEvidence.IsVisible = true;
            return;
        }
        foreach (var evidence in _dossiers.GetEvidence(_activeAudit.Id, _activeAuditPageId))
            InspectionEvidence.Add(new InspectionEvidenceItem(evidence.Id, evidence.CaptureMethod,
                $"{evidence.SelectedElementTag}{(string.IsNullOrWhiteSpace(evidence.SelectedElementText) ? string.Empty : " · " + evidence.SelectedElementText)} · {(evidence.ImageData is { Length: > 0 } ? $"{evidence.ImageMimeType} · {evidence.ImageData.Length / 1024} Ko" : "Extrait texte/HTML")}"));
        NoInspectionEvidence.IsVisible = InspectionEvidence.Count == 0;
    }

    private void RefreshAudits()
    {
        Audits.Clear();
        foreach (var manifest in _dossiers.List(includeArchived: _showArchivedAudits))
        {
            var level = string.IsNullOrEmpty(manifest.Level) ? string.Empty : $" · {manifest.Level}";
            var version = manifest.Version.StartsWith(manifest.Reference + " ", StringComparison.OrdinalIgnoreCase)
                ? manifest.Version[(manifest.Reference.Length + 1)..]
                : manifest.Version;
            var summary = $"{manifest.Reference} {version}{level}";
            var updated = $"Mis à jour {manifest.UpdatedAt.ToLocalTime():g}";
            Audits.Add(new AuditListItem(manifest.Id, new Uri(manifest.SiteUrl).Host, summary, updated, manifest.IsArchived, LoadFavicon(manifest.Id)));
        }

        EmptyAudits.IsVisible = Audits.Count == 0;
        AuditListTitle.Text = _showArchivedAudits ? "Audits archivés" : "Mes audits";
        AuditListSubtitle.Text = _showArchivedAudits
            ? "Les dossiers archivés peuvent être restaurés depuis leur menu d’actions."
            : "Ouvrez un dossier pour poursuivre votre audit.";
        ArchivedAuditsButton.Content = _showArchivedAudits ? "Audits actifs" : "Archives";
        ArchivedAuditsButton.SetValue(Avalonia.Automation.AutomationProperties.NameProperty,
            _showArchivedAudits ? "Afficher les audits actifs" : "Afficher les audits archivés");
        AuditCount.Text = Audits.Count == 1 ? "1 dossier" : $"{Audits.Count} dossiers";
        AuditCountTitle.Text = _showArchivedAudits ? "Audits archivés sur cet appareil" : "Audits actifs sur cet appareil";
        EmptyAuditTitle.Text = _showArchivedAudits ? "Aucun audit archivé" : "Aucun dossier d’audit";
        EmptyAuditDescription.Text = _showArchivedAudits
            ? "Les audits archivés apparaîtront ici. Vous pourrez les restaurer à tout moment."
            : "Créez un dossier pour définir le site et le référentiel avant de commencer.";
    }

    private Bitmap? LoadFavicon(string auditId)
    {
        var bytes = _dossiers.GetFaviconData(auditId);
        if (bytes is not { Length: > 0 })
            return null;
        try { return new Bitmap(new MemoryStream(bytes)); }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or NotSupportedException) { return null; }
    }

    private static byte[]? NormalizeFavicon(byte[]? source) =>
        source is { Length: > 0 } ? SiteDiscoveryService.NormalizeFavicon(source) : null;

    private void HomeView_SizeChanged(object? sender, SizeChangedEventArgs e) => UpdateHomeLayout(e.NewSize.Width);

    private void UpdateHomeLayout(double? availableWidth = null)
    {
        var width = availableWidth ?? HomeView.Bounds.Width;
        if (width > 0)
            HomeLayout.Width = Math.Min(1260, width);
    }

    private void WorkspaceView_SizeChanged(object? sender, SizeChangedEventArgs e) => UpdateWorkspaceLayout(e.NewSize.Width);

    private void UpdateWorkspaceLayout(double? availableWidth = null)
    {
        var width = availableWidth ?? WorkspaceView.Bounds.Width;
        if (width <= 0)
            return;

        var showPageScope = width >= 1120;
        PageScopePanel.IsVisible = showPageScope && !_pageScopeCollapsed;
        CollapsedPageTab.IsVisible = showPageScope && _pageScopeCollapsed;
        WorkspaceColumns.ColumnDefinitions[0].Width = !showPageScope
            ? new GridLength(0)
            : new GridLength(_pageScopeCollapsed ? 44 : 220);
        WorkspaceColumns.ColumnDefinitions[1].Width = showPageScope ? new GridLength(12) : new GridLength(0);
        var showSidePanel = _sidePanelShown && _pageViewMode != "dashboard";
        SidePanel.IsVisible = showSidePanel;
        WorkspaceColumns.ColumnDefinitions[3].Width = showSidePanel ? new GridLength(12) : new GridLength(0);
        WorkspaceColumns.ColumnDefinitions[4].Width = showSidePanel ? new GridLength(350) : new GridLength(0);
    }

    private void CollapsePageScope_Click(object? sender, RoutedEventArgs e)
    {
        _pageScopeCollapsed = true;
        PersistCurrentUserPreferences();
        UpdateWorkspaceLayout();
        ExpandPagesTabButton.Focus();
    }

    private void ExpandPageScope_Click(object? sender, RoutedEventArgs e)
    {
        _pageScopeCollapsed = false;
        PersistCurrentUserPreferences();
        UpdateWorkspaceLayout();
        CollapsePageScopeButton.Focus();
    }

    private void NewAudit_Click(object? sender, RoutedEventArgs e)
    {
        OptionsPopup.IsOpen = false;
        RefreshReferenceOptions();
        _activeAudit = null;
        _addingPagesToExistingAudit = false;
        _existingAuditIdForPageSelection = null;
        SetupBackButton.Content = "‹  Mes audits";
        SetupTitle.Text = "Créer un audit";
        SetupSubtitle.Text = "Définissez le site, puis choisissez le référentiel à appliquer.";
        SiteUrlBox.Text = string.Empty;
        ReferenceCombo.SelectedItem = null;
        VersionCombo.SelectedItem = null;
        LevelCombo.SelectedItem = null;
        VersionPanel.IsVisible = false;
        LevelPanel.IsVisible = false;
        VersionOptions.Clear();
        PriorityPageChoices.Clear();
        OtherPageChoices.Clear();
        _pendingFavicon = null;
        SetupConfiguration.IsVisible = true;
        SetupPageSelection.IsVisible = false;
        PageSelectionActions.IsVisible = false;
        PageSelectionStatus.IsVisible = false;
        SetupStatus.Text = string.Empty;
        SetupStatus.IsVisible = false;
        UpdateSetupGuidance();
        HomeView.IsVisible = false;
        WorkspaceView.IsVisible = false;
        SetupView.IsVisible = true;
        ConfirmPageSelectionButton.Content = "Valider et créer l’audit";
        OtherPagesHeading.Text = "Autres pages (25 maximum)";
        UpdateProjectListState();
        SiteUrlBox.Focus();
    }

    private async void ScanSite_Click(object? sender, RoutedEventArgs e)
    {
        var reference = ReferenceCombo.SelectedItem as string;
        var version = VersionCombo.SelectedItem as string;
        var siteUrl = SiteUrlBox.Text?.Trim() ?? string.Empty;
        if (reference is null || version is null)
        {
            SetupStatus.Text = "Choisissez un référentiel et une version disponibles avant l’analyse.";
            SetupStatus.IsVisible = true;
            return;
        }

        if (sender is Button button)
            button.IsEnabled = false;
        SetupStatus.Text = "Analyse en cours : recherche sur la page d’accueil et dans le sitemap public…";
        SetupStatus.IsVisible = true;
        try
        {
            _ = SiteDiscoveryService.ValidateOrigin(siteUrl);
            var scanTask = _siteDiscovery.ScanAsync(siteUrl);
            var faviconTask = FaviconConsentCheckBox.IsChecked == true
                ? _siteDiscovery.FetchFaviconAsync(siteUrl)
                : Task.FromResult<byte[]?>(null);
            var scan = await scanTask;
            _pendingFavicon = NormalizeFavicon(await faviconTask);
            PriorityPageChoices.Clear();
            OtherPageChoices.Clear();
            foreach (var page in scan.PriorityPages)
                PriorityPageChoices.Add(new SitePageChoice(page));
            foreach (var page in scan.OtherPages)
                OtherPageChoices.Add(new SitePageChoice(page));

            SetupConfiguration.IsVisible = false;
            SetupPageSelection.IsVisible = true;
            PageSelectionActions.IsVisible = true;
            PageSelectionStatus.IsVisible = false;
            SetupStatus.IsVisible = false;
            PageScanSummary.Text = scan.TotalCount == 0
                ? "Aucune page candidate n’a été trouvée. Vous pouvez relancer l’analyse ou vérifier l’URL."
                : $"{scan.PriorityPages.Count} page(s) prioritaire(s) et {scan.OtherPages.Count} autre(s) candidate(s). Le scan n’est pas exhaustif ; choisissez les pages à inclure.";
            NoPriorityPages.IsVisible = scan.PriorityPages.Count == 0;
            NoOtherPages.IsVisible = scan.OtherPages.Count == 0;
            FaviconScanStatus.Text = FaviconConsentCheckBox.IsChecked != true
                ? "Récupération du favicon désactivée."
                : _pendingFavicon is null ? "Favicon indisponible ; l’icône locale sera utilisée." : "Favicon récupéré pour ce dossier.";
        }
        catch (ArgumentException exception)
        {
            SetupStatus.Text = exception.Message;
            SetupStatus.IsVisible = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException)
        {
            SetupStatus.Text = "Le site n’a pas pu être analysé. Vérifiez l’URL et la connexion, puis réessayez.";
            SetupStatus.IsVisible = true;
        }
        finally
        {
            if (sender is Button scanButton)
                scanButton.IsEnabled = true;
        }
    }

    private void CancelPageSelection_Click(object? sender, RoutedEventArgs e)
    {
        if (_addingPagesToExistingAudit && _existingAuditIdForPageSelection is { } auditId && _dossiers.Get(auditId) is { } audit)
        {
            _addingPagesToExistingAudit = false;
            _existingAuditIdForPageSelection = null;
            OpenAudit(audit);
            return;
        }
        Projects_Click(sender, e);
    }

    private async void FindAdditionalPages_Click(object? sender, RoutedEventArgs e) => await FindAdditionalPages_ClickAsync(sender);

    private async Task FindAdditionalPages_ClickAsync(object? sender)
    {
        if (_activeAudit is null)
            return;

        _addingPagesToExistingAudit = true;
        _existingAuditIdForPageSelection = _activeAudit.Id;
        var audit = _activeAudit;
        var existingPages = _dossiers.GetPages(audit.Id);
        var remainingCapacity = _dossiers.GetRemainingAdditionalPageCapacity(audit.Id);
        PriorityPageChoices.Clear();
        OtherPageChoices.Clear();
        if (sender is Button button)
            button.IsEnabled = false;
        try
        {
            var scan = await _siteDiscovery.ScanAsync(
                audit.SiteUrl,
                existingPages.Select(page => page.Url).ToArray(),
                remainingCapacity);
            DisplayPageCandidates(scan);
            SetupBackButton.Content = "‹  Retour à l’audit";
            SetupTitle.Text = "Ajouter des pages";
            SetupSubtitle.Text = $"Sélectionnez de nouvelles pages pour {new Uri(audit.SiteUrl).Host}. Les pages déjà incluses sont masquées.";
            SetupConfiguration.IsVisible = false;
            SetupPageSelection.IsVisible = true;
            PageSelectionActions.IsVisible = true;
            PageSelectionStatus.IsVisible = false;
            SetupView.IsVisible = true;
            WorkspaceView.IsVisible = false;
            HomeView.IsVisible = false;
            UpdateProjectListState();
            PageScanSummary.Text = scan.TotalCount == 0
                ? "Aucune nouvelle page candidate n’a été trouvée. Les pages déjà incluses sont exclues de cette liste."
                : $"{scan.PriorityPages.Count} page(s) prioritaire(s) et {scan.OtherPages.Count} autre(s) nouvelle(s) candidate(s). Les pages déjà incluses sont exclues.";
            OtherPagesHeading.Text = remainingCapacity == 0
                ? "Autres pages — limite atteinte"
                : $"Autres pages — {remainingCapacity} ajout(s) possible(s)";
            NoOtherPages.Text = remainingCapacity == 0
                ? "La limite de 25 pages supplémentaires de cet audit est atteinte."
                : "Aucune autre nouvelle page candidate détectée.";
            FaviconScanStatus.Text = "Le favicon et le référentiel de cet audit restent inchangés.";
            SetupGuidanceTitle.Text = $"Audit {audit.Reference}";
            SetupGuidanceText.Text = $"Pages déjà incluses : {existingPages.Count}. Les candidates existantes ne sont pas reproposées.";
            ConfirmPageSelectionButton.Content = "Ajouter à l’audit";
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException)
        {
            SetupBackButton.Content = "‹  Retour à l’audit";
            SetupTitle.Text = "Ajouter des pages";
            SetupSubtitle.Text = $"Sélectionnez de nouvelles pages pour {new Uri(audit.SiteUrl).Host}.";
            SetupConfiguration.IsVisible = false;
            SetupPageSelection.IsVisible = true;
            PageSelectionActions.IsVisible = true;
            SetupView.IsVisible = true;
            WorkspaceView.IsVisible = false;
            HomeView.IsVisible = false;
            PageScanSummary.Text = "Le site n’a pas pu être analysé. Vérifiez la connexion puis relancez l’ajout de pages.";
            PageSelectionStatus.Text = PageScanSummary.Text;
            PageSelectionStatus.IsVisible = true;
            FaviconScanStatus.Text = string.Empty;
            NoPriorityPages.IsVisible = true;
            NoOtherPages.IsVisible = true;
        }
        finally
        {
            if (sender is Button scanButton)
                scanButton.IsEnabled = true;
        }
    }

    private void DisplayPageCandidates(SiteScanResult scan)
    {
        PriorityPageChoices.Clear();
        OtherPageChoices.Clear();
        foreach (var page in scan.PriorityPages)
            PriorityPageChoices.Add(new SitePageChoice(page));
        foreach (var page in scan.OtherPages)
            OtherPageChoices.Add(new SitePageChoice(page));
        NoPriorityPages.IsVisible = scan.PriorityPages.Count == 0;
        NoOtherPages.IsVisible = scan.OtherPages.Count == 0;
    }

    private void ConfirmPages_Click(object? sender, RoutedEventArgs e)
    {
        var selectedPages = PriorityPageChoices.Concat(OtherPageChoices)
            .Where(page => page.IsSelected)
            .Select(page => page.Candidate)
            .ToArray();
        try
        {
            if (_addingPagesToExistingAudit && _existingAuditIdForPageSelection is { } auditId)
            {
                var addedCount = _dossiers.AddPagesToAudit(auditId, selectedPages);
                if (addedCount == 0)
                    throw new ArgumentException("Aucune nouvelle page n’a été sélectionnée.");
                var updatedAudit = _dossiers.Get(auditId)
                    ?? throw new InvalidOperationException("Le dossier d’audit n’existe plus.");
                _addingPagesToExistingAudit = false;
                _existingAuditIdForPageSelection = null;
                OpenAudit(updatedAudit);
                return;
            }

            var manifest = _dossiers.Create(new NewAudit(
                SiteUrlBox.Text ?? string.Empty,
                ReferenceCombo.SelectedItem as string ?? string.Empty,
                VersionCombo.SelectedItem as string ?? string.Empty,
                LevelCombo.SelectedItem as string), selectedPages, _pendingFavicon);
            OpenAudit(manifest);
        }
        catch (ArgumentException exception)
        {
            PageScanSummary.Text = exception.Message;
            PageSelectionStatus.Text = exception.Message;
            PageSelectionStatus.IsVisible = true;
        }
        catch (InvalidOperationException exception)
        {
            PageScanSummary.Text = exception.Message;
            PageSelectionStatus.Text = exception.Message;
            PageSelectionStatus.IsVisible = true;
        }
        catch (IOException)
        {
            PageScanSummary.Text = "Le dossier n’a pas pu être enregistré. Vérifiez l’accès au stockage local puis réessayez.";
            PageSelectionStatus.Text = PageScanSummary.Text;
            PageSelectionStatus.IsVisible = true;
        }
    }

    private void Reference_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (VersionCombo is null || LevelPanel is null)
            return;

        var reference = ReferenceCombo.SelectedItem as string;
        VersionOptions.Clear();
        VersionCombo.SelectedItem = null;
        LevelCombo.SelectedItem = null;
        VersionPanel.IsVisible = reference is not null;
        LevelPanel.IsVisible = false;
        if (reference is not null)
        {
            foreach (var version in _dossiers.GetReferenceVersions()
                         .Where(item => string.Equals(item.FrameworkName, reference, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(item => item.VersionCode, StringComparer.OrdinalIgnoreCase))
            {
                VersionOptions.Add($"{version.FrameworkName} {version.VersionCode}");
            }
        }
        UpdateSetupGuidance();
    }

    private void RefreshReferenceOptions()
    {
        var selected = ReferenceCombo.SelectedItem as string;
        ReferenceOptions.Clear();
        foreach (var framework in _dossiers.GetReferenceVersions()
                     .GroupBy(item => item.FrameworkName, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First().FrameworkName)
                     .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
        {
            ReferenceOptions.Add(framework);
        }

        if (selected is not null && ReferenceOptions.Contains(selected))
            ReferenceCombo.SelectedItem = selected;
        else
            ReferenceCombo.SelectedItem = null;
    }

    private void Version_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (VersionCombo is null || LevelPanel is null)
            return;

        LevelCombo.SelectedItem = null;
        LevelPanel.IsVisible = ReferenceCombo.SelectedItem as string == "WCAG" && VersionCombo.SelectedItem is not null;
        UpdateSetupGuidance();
    }

    private void UpdateSetupGuidance()
    {
        var reference = ReferenceCombo.SelectedItem as string;
        var version = VersionCombo.SelectedItem as string;
        var hasCatalog = _dossiers.GetReferenceVersions().Count > 0;
        (SetupGuidanceTitle.Text, SetupGuidanceText.Text) = reference switch
        {
            "WCAG" when version is not null => ("Cible WCAG", "Le niveau A, AA ou AAA définit la cible et non le résultat de l’audit."),
            "WCAG" => ("Référentiel WCAG", "Choisissez une version WCAG installée ; le choix du niveau cible apparaîtra ensuite."),
            not null when version is not null => ($"Référentiel {reference}", $"La version {version} est choisie."),
            not null => ($"Référentiel {reference}", hasCatalog ? "Choisissez une version disponible dans la base locale." : "Aucun référentiel n’est encore installé dans la base locale."),
            _ => ("Un référentiel par dossier", hasCatalog ? "Choisissez un référentiel installé dans la base locale." : "Aucun référentiel n’est encore installé dans la base locale.")
        };
    }

    private void ResumeAudit_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not string id)
            return;

        var manifest = _dossiers.Get(id);
        if (manifest is not null)
            OpenAudit(manifest);
        else
        {
            RefreshAudits();
            ShowHome();
        }
    }

    private void ToggleArchivedAudits_Click(object? sender, RoutedEventArgs e)
    {
        _showArchivedAudits = !_showArchivedAudits;
        RefreshAudits();
    }

    private void AuditActions_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string id)
            return;
        var audit = Audits.FirstOrDefault(item => item.Id == id);
        if (audit is null)
            return;

        var flyout = new MenuFlyout();
        var archiveAction = new MenuItem { Header = audit.IsArchived ? "Restaurer" : "Archiver" };
        archiveAction.Click += (_, _) =>
        {
            _dossiers.SetArchived(id, !audit.IsArchived);
            RefreshAudits();
        };
        var deleteAction = new MenuItem { Header = "Supprimer…" };
        deleteAction.Click += async (_, _) => await ConfirmDeleteAuditAsync(id, audit.Title);
        flyout.Items.Add(archiveAction);
        flyout.Items.Add(new Separator());
        flyout.Items.Add(deleteAction);
        flyout.ShowAt(button);
    }

    private async Task<bool> ShowDestructiveConfirmationAsync(string heading, string title, string description, string actionLabel)
    {
        var cancelButton = new Button { Content = "Annuler", IsCancel = true, IsDefault = true, Height = 40 };
        var warningIcon = new Avalonia.Controls.Shapes.Path
        {
            Data = new GeometryGroup
            {
                Children =
                {
                    Geometry.Parse("M 10.29,3.86 C 11.05,2.58 12.95,2.58 13.71,3.86 L 22.18,18 C 22.94,19.28 21.98,21 20.47,21 H 3.53 C 2.02,21 1.06,19.28 1.82,18 Z"),
                    Geometry.Parse("M 12,9 L 12,13"),
                    new EllipseGeometry { Center = new Point(12, 17), RadiusX = 0.5, RadiusY = 0.5 }
                }
            },
            Fill = Brushes.Transparent,
            Stroke = Brush.Parse("#6A5016"),
            StrokeThickness = 2,
            Width = 18,
            Height = 18,
            Margin = new Thickness(0, 0, 12, 0)
        };
        var warningText = new TextBlock
        {
            Text = actionLabel,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = Brush.Parse("#6A5016")
        };
        var warningContent = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { warningIcon, warningText }
        };
        var warningSurface = new Border
        {
            Background = Brush.Parse("#FFF4D9"),
            BorderBrush = Brush.Parse("#C9A848"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16, 10),
            Child = warningContent
        };
        var confirmButton = new Button
        {
            Height = 44,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Content = warningSurface
        };
        confirmButton.SetValue(AutomationProperties.NameProperty, $"Attention, {actionLabel.ToLowerInvariant()}");
        confirmButton.SetValue(AutomationProperties.HelpTextProperty, description);
        var closeButton = new Button
        {
            Width = 36,
            Height = 36,
            Padding = new Thickness(0),
            Background = Brush.Parse("#FFFDF7"),
            BorderBrush = Brush.Parse("#7B8190"),
            BorderThickness = new Thickness(1),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Content = new Avalonia.Controls.Shapes.Path
            {
                Data = Geometry.Parse("M 5,5 L 19,19 M 19,5 L 5,19"),
                Stroke = Brush.Parse("#171F3B"),
                StrokeThickness = 2,
                Width = 14,
                Height = 14
            }
        };
        closeButton.SetValue(AutomationProperties.NameProperty, "Fermer la confirmation sans supprimer l’audit");
        var headerContent = new Grid { ColumnDefinitions = new ColumnDefinitions("*,40") };
        var headerTitle = new TextBlock
        {
            Text = heading,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#171F3B"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(closeButton, 1);
        headerContent.Children.Add(headerTitle);
        headerContent.Children.Add(closeButton);
        var header = new Border
        {
            Padding = new Thickness(20, 16),
            Background = Brush.Parse("#FFFDF7"),
            BorderBrush = Brush.Parse("#D8DAD9"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = headerContent
        };
        var body = new StackPanel
        {
            Margin = new Thickness(20, 18),
            Spacing = 8,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Brush.Parse("#171F3B"), TextWrapping = TextWrapping.Wrap },
                new TextBlock
                {
                    Text = description,
                    FontSize = 13,
                    Foreground = Brush.Parse("#45536D"),
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
        var footer = new Border
        {
            Padding = new Thickness(16),
            BorderBrush = Brush.Parse("#D8DAD9"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = new StackPanel { Spacing = 8, Children = { confirmButton, cancelButton } }
        };
        var dialogContent = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        Grid.SetRow(body, 1);
        Grid.SetRow(footer, 2);
        dialogContent.Children.Add(header);
        dialogContent.Children.Add(body);
        dialogContent.Children.Add(footer);
        confirmButton.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        confirmButton.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        cancelButton.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        cancelButton.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left;

        var pointerOverDelete = false;
        var deleteHasFocus = false;
        void UpdateDeleteEmphasis()
        {
            var emphasized = pointerOverDelete || deleteHasFocus;
            warningSurface.Background = Brush.Parse(emphasized ? "#FBE5E2" : "#FFF4D9");
            warningSurface.BorderBrush = Brush.Parse(emphasized ? "#B42318" : "#C9A848");
            warningIcon.Stroke = Brush.Parse(emphasized ? "#B42318" : "#6A5016");
            warningText.Foreground = Brush.Parse(emphasized ? "#B42318" : "#6A5016");
        }
        confirmButton.PointerEntered += (_, _) => { pointerOverDelete = true; UpdateDeleteEmphasis(); };
        confirmButton.PointerExited += (_, _) => { pointerOverDelete = false; UpdateDeleteEmphasis(); };
        confirmButton.GotFocus += (_, _) => { deleteHasFocus = true; UpdateDeleteEmphasis(); };
        confirmButton.LostFocus += (_, _) => { deleteHasFocus = false; UpdateDeleteEmphasis(); };

        var dialog = new Window
        {
            Title = heading,
            Width = 480,
            Height = 330,
            MinWidth = 480,
            MinHeight = 330,
            CanResize = false,
            WindowDecorations = WindowDecorations.None,
            ShowInTaskbar = false,
            Background = Brush.Parse("#EEF2FA"),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Margin = new Thickness(12),
                Background = Brush.Parse("#FFFDF7"),
                BorderBrush = Brush.Parse("#D8DAD9"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = dialogContent
            }
        };
        closeButton.Click += (_, _) => dialog.Close(false);
        confirmButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);
        header.PointerPressed += (_, args) =>
        {
            if (args.GetCurrentPoint(dialog).Properties.IsLeftButtonPressed)
                dialog.BeginMoveDrag(args);
        };
        return await dialog.ShowDialog<bool>(this);
    }

    private async Task ConfirmDeleteAuditAsync(string id, string title)
    {
        var confirmation = await ShowDestructiveConfirmationAsync(
            "Supprimer cet audit ?",
            title,
            "L’audit, ses pages, ses décisions et ses preuves seront supprimés définitivement. Cette action ne peut pas être annulée.",
            "Supprimer définitivement l’audit");
        if (!confirmation)
            return;

        if (_dossiers.DeleteAudit(id))
        {
            foreach (var tab in OpenAuditTabs.Where(tab => tab.Id == id).ToArray())
                OpenAuditTabs.Remove(tab);
            if (_activeAudit?.Id == id)
            {
                SetInspection(false);
                _activeAudit = null;
            }
            for (var index = 0; index < OpenAuditTabs.Count; index++)
                OpenAuditTabs[index] = OpenAuditTabs[index] with { IsActive = false };
        }
        RefreshAudits();
    }

    private async Task ConfirmDeletePageAsync(string auditId, AuditPageListItem page)
    {
        var confirmation = await ShowDestructiveConfirmationAsync(
            "Supprimer cette page ?",
            page.Name,
            "La page et les résultats, suggestions et constats qui lui sont associés seront supprimés définitivement.",
            "Supprimer définitivement la page");
        if (!confirmation)
            return;

        if (_dossiers.RemovePageFromAudit(auditId, page.Id))
        {
            if (_activeAuditPageId == page.Id)
            {
                _activeAuditPageId = null;
                LoadLocalFixture();
                if (_activeAudit is not null)
                    CurrentSiteHeader.Text = $"Site enregistré dans ce dossier : {_activeAudit.SiteUrl}";
            }
            RefreshAuditPages(auditId);
            _activeAudit = _dossiers.Get(auditId) ?? _activeAudit;
        }
    }

    private void OpenAudit(AuditManifest manifest)
    {
        if (_contrastPipetteActive)
            _ = StopContrastPipetteAsync("Dossier changé ; pipette arrêtée.");
        if (_activeAudit?.Id != manifest.Id && _inspectionActive)
            SetInspection(false);

        _activeAudit = manifest;
        _activeAuditPageId = null;
        _pageSourceSnapshot = null;
        _siteStylesRemoved = false;
        _addingPagesToExistingAudit = false;
        _existingAuditIdForPageSelection = null;
        RefreshAuditPages(manifest.Id);
        AddOrActivateTab(manifest);
        Dispatcher.UIThread.Post(() => EnsureOpenTabVisible(manifest.Id), DispatcherPriority.Loaded);
        UpdateWindowActivation(IsActive);
        CurrentAuditHeader.Text = $"{manifest.Title} · {manifest.Version}{(string.IsNullOrEmpty(manifest.Level) ? string.Empty : $" · {manifest.Level}")}";
        CurrentSiteHeader.Text = $"Site enregistré dans ce dossier : {manifest.SiteUrl}";
        SetupView.IsVisible = false;
        HomeView.IsVisible = false;
        WorkspaceView.IsVisible = true;
        UpdateWorkspaceLayout();
        UpdateProjectListState();
        RestoreReviewRecord();
        AxeEvidencePanel.IsVisible = false;
        ColorContrastPanel.IsVisible = false;
        _contrastReviewContext = null;
        RefreshInspectionEvidence();
        ShowAuditOverview();
    }

    private void RefreshAuditPages(string auditId)
    {
        AuditPages.Clear();
        foreach (var page in _dossiers.GetPages(auditId))
        {
        var pendingSuggestions = page.Suggestions.Count(item => item.AnalyzerVersion.StartsWith("axe-core ", StringComparison.Ordinal) && item.ReviewStatus == "À vérifier");
            AuditPages.Add(new AuditPageListItem(page.Id, page.Name, FormatLocalPagePath(page.Url), page.SamplingReason,
                page.Id == _activeAuditPageId, page.LastOpenedAtUtc is not null, page.TestResults.Count, pendingSuggestions));
        }
        PageCountText.Text = AuditPages.Count == 1 ? "1 page incluse" : $"{AuditPages.Count} pages incluses";
        EmptyAuditPages.IsVisible = AuditPages.Count == 0;
    }

    private void AuditOverview_Click(object? sender, RoutedEventArgs e) => ShowAuditOverview();

    private void ShowAuditOverview()
    {
        if (_activeAudit is null) return;
        if (_contrastPipetteActive)
            _ = StopContrastPipetteAsync("Retour au tableau de bord ; pipette arrêtée.");
        _activeAuditPageId = null;
        RefreshAuditPages(_activeAudit.Id);
        AxeResults.Clear();
        RefreshAxeResults();
        AxeEvidencePanel.IsVisible = false;
        ColorContrastPanel.IsVisible = false;
        _contrastReviewContext = null;
        RefreshInspectionEvidence();
        SetPageView("dashboard");
        DashboardSubtitle.Text = $"{_activeAudit.SiteUrl} · {_activeAudit.Version}{(string.IsNullOrWhiteSpace(_activeAudit.Level) ? string.Empty : $" · niveau {_activeAudit.Level}")}";
        CurrentSiteHeader.Text = $"Vue d’ensemble · {FormatAuditPageCount(_dossiers.GetPages(_activeAudit.Id).Count)}";
        BrowserModeLabel.Text = "Vue d’ensemble de l’audit";
        BrowserModeBorder.SetValue(AutomationProperties.NameProperty, "Vue d’ensemble du dossier d’audit");
        BrowserSafetyNote.Text = "Les indicateurs résument les pages ouvertes et les éléments réellement enregistrés dans ce dossier.";
        AuditOverviewButton.Tag = true;
        RefreshAuditDashboard();
    }

    private static string FormatAuditPageCount(int count) => count == 1 ? "1 page" : $"{count} pages";

    private void RefreshAuditDashboard()
    {
        DashboardMetrics.Clear();
        if (_activeAudit is null)
        {
            DashboardPageProgressText.Text = string.Empty;
            DashboardPageProgress.Value = 0;
            DashboardNextSteps.Text = "Ouvrez un dossier d’audit pour afficher son avancement.";
            DashboardOpenNextPageButton.IsEnabled = false;
            DashboardReviewSuggestionsButton.IsVisible = false;
            return;
        }

        var pages = _dossiers.GetPages(_activeAudit.Id);
        var openedPages = pages.Count(page => page.LastOpenedAtUtc is not null);
        var testResults = pages.SelectMany(page => page.TestResults).ToArray();
        var pendingTests = testResults.Count(result => result.Status == "À vérifier");
        var decisions = _dossiers.ReadDecisions(_activeAudit.Id).Count(decision => !decision.ReviewComplete);
        var findings = pages.SelectMany(page => page.Findings).ToArray();
        var findingsToFollow = findings.Count(finding => finding.Status is not ("Corrigé" or "Résolu" or "Fermé"));
        var pendingSuggestions = pages.SelectMany(page => page.Suggestions)
            .Count(suggestion => suggestion.AnalyzerVersion.StartsWith("axe-core ", StringComparison.Ordinal) && suggestion.ReviewStatus == "À vérifier");
        var evidenceCount = _dossiers.GetEvidenceCount(_activeAudit.Id);

        DashboardMetrics.Add(new AuditDashboardMetric("Pages ouvertes", $"{openedPages} / {pages.Count}", "Pages du lot consultées dans l’outil"));
        DashboardMetrics.Add(new AuditDashboardMetric("Contrôles consignés", testResults.Length.ToString(), $"{pendingTests} encore marqués « À vérifier »"));
        DashboardMetrics.Add(new AuditDashboardMetric("Décisions humaines", decisions.ToString(), "Résultats enregistrés par l’auditeur"));
        DashboardMetrics.Add(new AuditDashboardMetric("Constats", findings.Length.ToString(), $"{findingsToFollow} à suivre"));
        DashboardMetrics.Add(new AuditDashboardMetric("Suggestions axe-core", pendingSuggestions.ToString(), "À vérifier manuellement"));
        DashboardMetrics.Add(new AuditDashboardMetric("Preuves", evidenceCount.ToString(), "Enregistrées dans le dossier"));

        DashboardPageProgressText.Text = $"{openedPages} sur {pages.Count}";
        DashboardPageProgress.Value = pages.Count == 0 ? 0 : openedPages * 100d / pages.Count;

        var firstUnopened = pages.FirstOrDefault(page => page.LastOpenedAtUtc is null);
        var firstPendingSuggestionPage = pages.FirstOrDefault(page => page.Suggestions.Any(suggestion =>
            suggestion.AnalyzerVersion.StartsWith("axe-core ", StringComparison.Ordinal) && suggestion.ReviewStatus == "À vérifier"));
        DashboardOpenNextPageButton.IsEnabled = pages.Count > 0;
        DashboardOpenNextPageButton.Content = firstUnopened is null ? "Reprendre une page" : "Ouvrir une page non consultée";
        DashboardReviewSuggestionsButton.IsVisible = firstPendingSuggestionPage is not null;

        DashboardNextSteps.Text = firstUnopened is not null
            ? $"Il reste {pages.Count - openedPages} page(s) du lot à ouvrir. L’ouverture n’est pas un résultat d’audit : consigne les contrôles au fil de la vérification."
            : pendingSuggestions > 0
                ? $"{pendingSuggestions} suggestion(s) automatisée(s) attendent une décision humaine."
                : testResults.Length == 0 && decisions == 0
                    ? "Aucun résultat de contrôle manuel n’est encore enregistré. Ouvre une page pour commencer et consigner tes décisions dans Inspection."
                    : findingsToFollow > 0
                        ? $"{findingsToFollow} constat(s) restent à suivre dans ce dossier."
                        : "Les pages du lot ont été ouvertes. Vérifie les contrôles et constats enregistrés avant de conclure la revue.";
        RefreshAuditPages(_activeAudit.Id);
    }

    private void DashboardOpenNextPage_Click(object? sender, RoutedEventArgs e)
    {
        if (_activeAudit is null) return;
        var pages = _dossiers.GetPages(_activeAudit.Id);
        var page = pages.FirstOrDefault(candidate => candidate.LastOpenedAtUtc is null) ?? pages.FirstOrDefault();
        if (page is not null) OpenAuditPage(page.Id);
    }

    private void DashboardReviewSuggestions_Click(object? sender, RoutedEventArgs e)
    {
        if (_activeAudit is null) return;
        var page = _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(candidate => candidate.Suggestions.Any(suggestion =>
            suggestion.AnalyzerVersion.StartsWith("axe-core ", StringComparison.Ordinal) && suggestion.ReviewStatus == "À vérifier"));
        if (page is null) return;
        OpenAuditPage(page.Id);
        SetPageView("axe");
        BrowserModeLabel.Text = "Résultats axe-core";
        BrowserSafetyNote.Text = "Suggestions de l’analyse précédente à examiner ; elles ne constituent pas un verdict de conformité.";
        RefreshAxeResults();
        if (AxeResults.Count == 0)
            AxeScanStatus.Text = "Aucune suggestion en attente pour cette page.";
    }

    private static string FormatLocalPagePath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;
        var path = Uri.UnescapeDataString(uri.AbsolutePath);
        const int maxLength = 64;
        return path.Length <= maxLength ? path : path[..(maxLength - 1)] + "…";
    }

    private void AuditPage_Click(object? sender, RoutedEventArgs e)
    {
        if (_activeAudit is null || (sender as Button)?.Tag is not string pageId)
            return;
        OpenAuditPage(pageId);
    }

    private void OpenAuditPage(string pageId)
    {
        if (_activeAudit is null) return;
        var page = _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(candidate => candidate.Id == pageId);
        if (page is null)
            return;

        if (_inspectionActive)
            SetInspection(false);
        if (_contrastPipetteActive)
            _ = StopContrastPipetteAsync("La page a changé ; pipette arrêtée.");
        AxeEvidencePanel.IsVisible = false;
        ColorContrastPanel.IsVisible = false;
        _contrastReviewContext = null;
        _activeAuditPageId = page.Id;
        _dossiers.MarkPageOpened(_activeAudit.Id, page.Id);
        _activeAudit = _dossiers.Get(_activeAudit.Id) ?? _activeAudit;
        page = _dossiers.GetPages(_activeAudit.Id).First(candidate => candidate.Id == pageId);
        AuditOverviewButton.Tag = false;
        _selectedAxeSuggestion = null;
        _selectedAxeContext = null;
        _selectedAxeTarget = _selectedAxeTag = _selectedAxeText = _selectedAxeHtml = string.Empty;
        RefreshInspectionEvidence();
        _pageSourceSnapshot = null;
        _siteStylesRemoved = false;
        RefreshAuditPages(_activeAudit.Id);
        SetPageView("site");
        BrowserModeLabel.Text = "Page sélectionnée dans l’audit";
        BrowserModeBorder.SetValue(AutomationProperties.NameProperty, "Le navigateur affiche une page sélectionnée dans l’audit");
        BrowserSafetyNote.Text = "Page sélectionnée dans le périmètre de cet audit. La décision de conformité appartient à l’auditeur.";
        CurrentSiteHeader.Text = $"Page du dossier : {page.Name} · {FormatLocalPagePath(page.Url)}";
        Browser.Source = new Uri(page.Url);
        AxeResults.Clear();
        RefreshAxeResults();
        RestoreReviewRecord();
        RefreshAudits();
        UpdateInspectionMenuCommands();
    }

    private void ShowSiteView_Click(object? sender, RoutedEventArgs e)
    {
        var needsReload = _siteStylesRemoved;
        _siteStylesRemoved = false;
        SetPageView("site");
        if (_activeAuditPageId is { } pageId && _activeAudit is not null &&
            _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(page => page.Id == pageId) is { } page)
        {
            _pageSourceSnapshot = null;
            if (needsReload)
                Browser.Refresh();
            else
                Browser.Source = new Uri(page.Url);
            BrowserModeLabel.Text = "Page sélectionnée dans l’audit";
            BrowserSafetyNote.Text = "Page sélectionnée dans le périmètre de cet audit. La décision de conformité appartient à l’auditeur.";
        }
        else if (_activeAudit is not null)
        {
            ShowAuditOverview();
        }
        else
        {
            if (needsReload)
                Browser.Refresh();
            else
                LoadLocalFixture();
        }
    }

    private void PageViewTools_Click(object? sender, RoutedEventArgs e)
    {
        var menu = new MenuFlyout();
        var overview = new MenuItem { Header = "Vue d’ensemble de l’audit" };
        overview.Click += (_, _) => ShowAuditOverview();
        var site = new MenuItem { Header = "Site" };
        site.Click += (_, _) => ShowSiteView_Click(this, new RoutedEventArgs());
        var source = new MenuItem { Header = "Code source" };
        source.Click += async (_, _) => await ShowCodeSourceViewAsync();
        var noCss = new MenuItem { Header = "Site sans CSS" };
        noCss.Click += async (_, _) => await ShowNoCssViewAsync();
        var semantic = new MenuItem { Header = "HTML5 sémantique" };
        semantic.Click += async (_, _) => await ShowSemanticViewAsync();
        var media = new MenuItem { Header = "Images et médias" };
        media.Click += async (_, _) => await ShowMediaViewAsync();
        var ariaTree = new MenuItem { Header = "Vue ARIA (aperçu lecteur d’écran)" };
        ariaTree.Click += async (_, _) => await ShowAriaTreeViewAsync();
        var axeResults = new MenuItem { Header = "Résultats axe-core" };
        axeResults.Click += (_, _) => ShowAxeResultsView();
        var technologies = new MenuItem { Header = "Technologies de la page" };
        technologies.Click += async (_, _) => await ShowTechnologyProfileAsync();
        menu.Items.Add(overview);
        menu.Items.Add(site);
        menu.Items.Add(source);
        menu.Items.Add(noCss);
        menu.Items.Add(semantic);
        menu.Items.Add(media);
        menu.Items.Add(ariaTree);
        menu.Items.Add(technologies);
        menu.Items.Add(axeResults);
        menu.ShowAt(PageViewToolsButton);
    }

    private async void ShowNoCssView_Click(object? sender, RoutedEventArgs e)
        => await ShowNoCssViewAsync();

    private async Task ShowNoCssViewAsync()
    {
        SetPageView("no-css");
        BrowserModeLabel.Text = "Site sans CSS";
        BrowserSafetyNote.Text = "Les règles CSS sont masquées dans cette vue. Le contenu et les comportements scriptés du site peuvent rester actifs.";
        try
        {
            await Browser.InvokeScript("document.querySelectorAll('style,link[rel~=\\\"stylesheet\\\"]').forEach(e=>e.remove());document.querySelectorAll('[style]').forEach(e=>e.removeAttribute('style'));try{document.adoptedStyleSheets=[]}catch{}");
            await Browser.InvokeScript("document.querySelectorAll('svg[aria-hidden=\\\"true\\\"],[aria-hidden=\\\"true\\\"] svg,svg:not([width]):not([height])').forEach(s=>{s.setAttribute('width','16');s.setAttribute('height','16');s.style.setProperty('width','16px','important');s.style.setProperty('height','16px','important');s.style.setProperty('max-width','16px','important');s.style.setProperty('max-height','16px','important');s.style.setProperty('vertical-align','middle')})");
            await Browser.InvokeScript(ReplaceMediaWithBadgesScript);
            _siteStylesRemoved = true;
        }
        catch (Exception)
        {
            ShowPageViewError("La vue sans CSS n’a pas pu être appliquée à cette page.");
        }
    }

    private async void ShowCodeSourceView_Click(object? sender, RoutedEventArgs e)
        => await ShowCodeSourceViewAsync();

    private async Task ShowCodeSourceViewAsync()
    {
        SetPageView("source");
        BrowserModeLabel.Text = "Code source DOM";
        BrowserSafetyNote.Text = "Source DOM reçue par le navigateur après le chargement de la page.";
        try
        {
            var source = DecodeScriptString(_pageSourceSnapshot ?? await Browser.InvokeScript("document.documentElement.outerHTML"));
            if (string.IsNullOrEmpty(source))
            {
                ShowPageViewError("Le code source n’est pas disponible pour cette page.");
                return;
            }
            const int maxSourceLength = 1_000_000;
            var formattedSource = SiteDiscoveryService.FormatHtmlSource(source);
            SourceCodeView.Text = formattedSource.Length > maxSourceLength
                ? formattedSource[..maxSourceLength] + "\n<!-- Source formatée tronquée à 1 000 000 caractères. -->"
                : formattedSource;
        }
        catch (Exception)
        {
            ShowPageViewError("Le code source n’a pas pu être lu depuis cette page.");
        }
    }

    private async void ShowSemanticView_Click(object? sender, RoutedEventArgs e)
        => await ShowSemanticViewAsync();

    private async Task ShowSemanticViewAsync()
    {
        if (_siteStylesRemoved)
        {
            _siteStylesRemoved = false;
            if (!await RefreshBrowserAndWaitAsync())
            {
                ShowPageViewError("La page n’a pas pu être rechargée avant l’analyse sémantique.");
                return;
            }
        }
        SetPageView("semantic");
        BrowserModeLabel.Text = "Structure HTML5";
        BrowserSafetyNote.Text = "Balises HTML5 et titres. Les écarts détectés sont des pistes à vérifier, pas des décisions de conformité.";
        try
        {
            var json = DecodeScriptString(await Browser.InvokeScript(BuildSemanticTreeScript));
            var nodes = string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<SemanticTreeNode>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            var existingRules = _activeAudit is not null && _activeAuditPageId is { } activePageId
                ? _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(page => page.Id == activePageId)?.Findings
                    .Select(finding => finding.SourceRuleId).Where(rule => !string.IsNullOrEmpty(rule)).ToHashSet(StringComparer.Ordinal)
                    ?? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            SemanticTreeNodes.Clear();
            foreach (var node in nodes)
                SemanticTreeNodes.Add(node with
                {
                    IsAddedToReport = existingRules.Contains(node.RuleId),
                    IsPageSelected = _activeAudit is not null && _activeAuditPageId is not null
                });
            StructurePanelTitle.Text = "Structure HTML5";
            StructurePanelSubtitle.Text = "Balises sémantiques, titres et emplacements à vérifier.";
            NoStructureNodes.Text = "Aucune balise HTML5 sémantique ou aucun titre détecté.";
            NoStructureNodes.IsVisible = SemanticTreeNodes.Count == 0;
            SemanticReportStatus.Text = _activeAuditPageId is null
                ? "Ouvre une page du dossier pour pouvoir ajouter un constat à son rapport."
                : "Survole ou sélectionne une balise pour la repérer dans la page.";
        }
        catch (Exception)
        {
            ShowPageViewError("La structure sémantique n’a pas pu être analysée.");
        }
    }

    private async void ShowMediaView_Click(object? sender, RoutedEventArgs e) => await ShowMediaViewAsync();

    private async Task ShowMediaViewAsync()
    {
        if (_siteStylesRemoved)
        {
            _siteStylesRemoved = false;
            if (!await RefreshBrowserAndWaitAsync())
            {
                ShowPageViewError("La page n’a pas pu être rechargée avant l’analyse des médias.");
                return;
            }
        }
        SetPageView("media");
        BrowserModeLabel.Text = "Images et médias";
        BrowserSafetyNote.Text = "Repérage des images et médias. Les alertes sont des pistes à vérifier par l’auditeur.";
        try
        {
            var json = DecodeScriptString(await Browser.InvokeScript(BuildMediaTreeScript));
            var nodes = string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<SemanticTreeNode>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            var existingRules = _activeAudit is not null && _activeAuditPageId is { } activePageId
                ? _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(page => page.Id == activePageId)?.Findings
                    .Select(finding => finding.SourceRuleId).Where(rule => !string.IsNullOrEmpty(rule)).ToHashSet(StringComparer.Ordinal)
                    ?? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            SemanticTreeNodes.Clear();
            foreach (var node in nodes)
                SemanticTreeNodes.Add(node with
                {
                    IsAddedToReport = existingRules.Contains(node.RuleId),
                    IsPageSelected = _activeAudit is not null && _activeAuditPageId is not null
                });
            StructurePanelTitle.Text = "Images et médias";
            StructurePanelSubtitle.Text = "Images, SVG, vidéo et audio ; alternatives, noms et sous-titres à vérifier.";
            NoStructureNodes.Text = "Aucune image ou aucun média détecté dans cette page.";
            NoStructureNodes.IsVisible = SemanticTreeNodes.Count == 0;
            SemanticReportStatus.Text = _activeAuditPageId is null
                ? "Ouvre une page du dossier pour ajouter un constat à son rapport."
                : "Survole ou sélectionne un média pour le repérer dans la page.";
        }
        catch (Exception)
        {
            ShowPageViewError("Les images et médias n’ont pas pu être analysés.");
        }
    }

    private async void SemanticTreeNode_PointerEntered(object? sender, RoutedEventArgs e) => await HighlightSemanticNodeAsync(sender);
    private async void SemanticTreeNode_GotFocus(object? sender, RoutedEventArgs e) => await HighlightSemanticNodeAsync(sender);
    private async void SemanticTreeNode_PointerExited(object? sender, RoutedEventArgs e) => await ClearSemanticHighlightAsync();
    private async void SemanticTreeNode_LostFocus(object? sender, RoutedEventArgs e) => await ClearSemanticHighlightAsync();
    private async void SemanticTreeNode_Click(object? sender, RoutedEventArgs e) => await HighlightSemanticNodeAsync(sender);

    private async Task HighlightSemanticNodeAsync(object? sender)
    {
        if (sender is not Button { Tag: string nodeId } ||
            !(nodeId.StartsWith("semantic-", StringComparison.Ordinal) && int.TryParse(nodeId.AsSpan(9), out _) ||
              nodeId.StartsWith("media-", StringComparison.Ordinal) && int.TryParse(nodeId.AsSpan(6), out _)))
            return;
        var node = SemanticTreeNodes.FirstOrDefault(item => item.Id == nodeId);
        if (node is null)
            return;
        SelectionSummary.Text = $"{node.Badge}{(string.IsNullOrWhiteSpace(node.Summary) ? string.Empty : $" · {node.Summary}")}{(node.HasIssue ? $"\n{node.IssueLabel} : {node.Issue}" : string.Empty)}";
        try { await Browser.InvokeScript($"window.__rgaaStudioHighlightSemanticNode('{nodeId}')"); }
        catch (Exception) { }
    }

    private async Task ClearSemanticHighlightAsync()
    {
        if (_pageViewMode is not ("semantic" or "media"))
            return;
        try { await Browser.InvokeScript("window.__rgaaStudioClearSemanticHighlight()"); }
        catch (Exception) { }
    }

    private void AddSemanticFindingToReport_Click(object? sender, RoutedEventArgs e)
    {
        if (_activeAudit is null || _activeAuditPageId is null || (sender as Button)?.Tag is not string nodeId)
        {
            SemanticReportStatus.Text = "Sélectionne d’abord une page du lot de l’audit.";
            return;
        }
        var node = SemanticTreeNodes.FirstOrDefault(item => item.Id == nodeId);
        if (node is null || !node.HasIssue)
            return;

        try
        {
            var added = _dossiers.AddSemanticFindingToAudit(
                _activeAudit.Id,
                _activeAuditPageId,
                node.RuleId,
                $"Vérifier {node.Badge} {node.Summary}".Trim(),
                node.Issue,
                node.Locator,
                node.RuleId.StartsWith("media.", StringComparison.Ordinal) ? "Analyse des images et médias" : "Analyse sémantique HTML5");
            if (added)
            {
                var index = SemanticTreeNodes.IndexOf(node);
                SemanticTreeNodes[index] = node with { IsAddedToReport = true };
                _activeAudit = _dossiers.Get(_activeAudit.Id) ?? _activeAudit;
                RefreshAudits();
            }
            SemanticReportStatus.Text = added
                ? "Constat ajouté à la page de l’audit pour examen par l’auditeur."
                : "Ce constat figure déjà dans cette page de l’audit.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            SemanticReportStatus.Text = exception.Message;
        }
    }

    private async void ShowAriaTreeView_Click(object? sender, RoutedEventArgs e)
        => await ShowAriaTreeViewAsync();

    private async Task ShowAriaTreeViewAsync()
    {
        if (_siteStylesRemoved)
        {
            _siteStylesRemoved = false;
            if (!await RefreshBrowserAndWaitAsync())
            {
                ShowPageViewError("La page n’a pas pu être rechargée avant la construction de la vue ARIA.");
                return;
            }
        }
        SetPageView("aria");
        BrowserModeLabel.Text = "Vue ARIA";
        BrowserSafetyNote.Text = "Aperçu des rôles et noms accessibles calculés depuis le DOM ; à confirmer avec un lecteur d’écran.";
        try
        {
            var json = DecodeScriptString(await Browser.InvokeScript(BuildAriaTreeScript));
            var nodes = string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<AriaTreeNode>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            AriaTreeNodes.Clear();
            foreach (var node in nodes)
                AriaTreeNodes.Add(node);
            NoAriaTreeNodes.IsVisible = AriaTreeNodes.Count == 0;
        }
        catch (Exception)
        {
            ShowPageViewError("L’arborescence ARIA n’a pas pu être construite pour cette page.");
        }
    }

    private void SetPageView(string mode)
    {
        if (_contrastPipetteActive && mode != _pageViewMode)
            _ = StopContrastPipetteAsync("Vue changée ; pipette arrêtée.");
        _pageViewMode = mode;
        var isDashboard = mode == "dashboard";
        PageViewToolsButton.IsVisible = !isDashboard;
        RunAxeScanButton.IsVisible = !isDashboard;
        PipetteButton.IsVisible = !isDashboard;
        DashboardToolbarLabel.IsVisible = isDashboard;
        PageViewToolsButton.Tag = mode == "site";
        PageViewToolsLabel.Text = mode switch
        {
            "source" => "Code source",
            "no-css" => "Site sans CSS",
            "semantic" => "HTML5 sémantique",
            "media" => "Images et médias",
            "aria" => "Vue ARIA",
            "technology" => "Technologies de la page",
            "axe" => "Résultats axe-core",
            _ => "Site"
        };
        Browser.IsVisible = mode is "site" or "no-css" or "aria" or "semantic" or "media" or "axe" or "technology";
        SourceCodeView.IsVisible = mode == "source";
        AuditDashboardPanel.IsVisible = isDashboard;
        AuditDashboardPanel.IsVisible = isDashboard;
        AriaTreePanel.IsVisible = mode == "aria";
        AxeResultsPanel.IsVisible = mode == "axe";
        TechnologyProfilePanel.IsVisible = mode == "technology";
        SemanticTreePanel.IsVisible = mode is "semantic" or "media";
        NoStructureNodes.IsVisible = (mode is "semantic" or "media") && SemanticTreeNodes.Count == 0;
        SemanticTreePanel.SetValue(AutomationProperties.NameProperty,
            mode == "media" ? "Arborescence des images et médias" : "Balises HTML5 et anomalies de placement");
        var treeView = mode is "aria" or "semantic" or "media" or "axe" or "technology";
        PageContentGrid.ColumnDefinitions[0].Width = treeView ? new GridLength(2, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);
        PageContentGrid.ColumnDefinitions[1].Width = treeView ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        PageViewStatus.IsVisible = false;
        UpdateInspectionMenuCommands();
        UpdateWorkspaceLayout();
        UpdateWorkspaceLayout();
    }

    private void ShowAxeResultsView()
    {
        SetPageView("axe");
        BrowserModeLabel.Text = "Résultats axe-core";
        BrowserSafetyNote.Text = "Les résultats automatisés sont des aides à l’examen ; ils ne constituent pas une décision de conformité.";
        RefreshAxeResults();
    }

    private async Task ShowTechnologyProfileAsync()
    {
        if (_siteStylesRemoved)
        {
            _siteStylesRemoved = false;
            if (!await RefreshBrowserAndWaitAsync())
            {
                ShowPageViewError("La page n’a pas pu être rechargée avant le résumé technique.");
                return;
            }
        }
        SetPageView("technology");
        BrowserModeLabel.Text = "Technologies de la page";
        BrowserSafetyNote.Text = "Résumé indicatif produit localement depuis les éléments déjà chargés dans la page.";
        await RefreshTechnologyProfileAsync();
    }

    private async void RefreshTechnologyProfile_Click(object? sender, RoutedEventArgs e) => await RefreshTechnologyProfileAsync();

    private async Task RefreshTechnologyProfileAsync()
    {
        TechnologyProfileStatus.Text = "Lecture des indices techniques chargés dans la page…";
        try
        {
            var json = DecodeScriptString(await Browser.InvokeScript(BuildPageProfileScript));
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("Le navigateur n’a pas retourné de profil technique.");
            var profile = JsonSerializer.Deserialize<TechnologyPageProfile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (profile is null) throw new JsonException("Le profil technique est vide.");
            var matches = PageTechnologyDetector.Detect(profile.Html, profile.Globals);
            TechnologyFindings.Clear();
            foreach (var match in matches)
                TechnologyFindings.Add(new TechnologyFinding(match.Name, match.Category, match.Evidence, match.Confidence, match.Version));
            NoTechnologyFindings.IsVisible = TechnologyFindings.Count == 0;
            TechnologyPageTitle.Text = string.IsNullOrWhiteSpace(profile.Title) ? "Page sans titre" : profile.Title;
            TechnologyPageFacts.Text = $"{profile.Url}\nLangue : {TextOr(profile.Language, "non définie")} · Scripts : {profile.ScriptCount} ({profile.ExternalScriptCount} externes, {Math.Max(0, profile.ScriptCount - profile.ExternalScriptCount)} inline) · CSS : {profile.StylesheetCount} · Images : {profile.ImageCount} · Formulaires : {profile.FormCount}";
            TechnologyDetectionSummary.Text = matches.Count == 0
                ? "Aucune signature reconnue dans les données DOM analysées."
                : $"{matches.Count} technologie(s) reconnue(s) parmi les signatures locales.";
            SeoFindings.Clear();
            foreach (var finding in BuildSeoFindings(profile.Seo ?? new SeoPageSnapshot()))
                SeoFindings.Add(finding);
            TechnologyProfileStatus.Text = TechnologyFindings.Count == 0
                ? "Détection locale actualisée · aucune correspondance dans le jeu de signatures connu."
                : $"Détection locale actualisée · {TechnologyFindings.Count} correspondance(s), à confirmer.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or JsonException or ArgumentException)
        {
            TechnologyFindings.Clear();
            SeoFindings.Clear();
            NoTechnologyFindings.IsVisible = true;
            TechnologyPageTitle.Text = string.Empty;
            TechnologyPageFacts.Text = string.Empty;
            TechnologyDetectionSummary.Text = string.Empty;
            TechnologyProfileStatus.Text = "Le résumé technique n’a pas pu être lu. Vérifiez que la page est chargée, puis actualisez.";
        }
    }

    private static List<SeoFinding> BuildSeoFindings(SeoPageSnapshot seo)
    {
        static SeoFinding Fact(string label, string value, bool present, string missingState = "Non renseigné") => new(
            label, string.IsNullOrWhiteSpace(value) ? missingState : value,
            string.IsNullOrWhiteSpace(value) ? missingState : present ? "Présent" : "À vérifier");

        var imageStatus = seo.ImageCount == 0 ? "Aucune image" : seo.ImagesMissingAlt == 0 ? $"Aucun alt manquant sur {seo.ImageCount}" : $"{seo.ImagesMissingAlt} sans alt sur {seo.ImageCount}";
        var structured = seo.StructuredDataTypes is { Count: > 0 } types ? string.Join(", ", types) : string.Empty;
        return
        [
            Fact("Titre de page", seo.Title, !string.IsNullOrWhiteSpace(seo.Title), "Absent"),
            Fact("Meta description", seo.Description, !string.IsNullOrWhiteSpace(seo.Description), "Absente"),
            Fact("URL canonique", seo.Canonical, !string.IsNullOrWhiteSpace(seo.Canonical)),
            Fact("Directive robots", seo.Robots, !string.IsNullOrWhiteSpace(seo.Robots), "Non définie · comportement par défaut"),
            Fact("Langue du document", seo.Language, !string.IsNullOrWhiteSpace(seo.Language), "Absente"),
            Fact("Encodage", seo.Charset, !string.IsNullOrWhiteSpace(seo.Charset), "Non déterminé"),
            Fact("Viewport mobile", seo.Viewport, !string.IsNullOrWhiteSpace(seo.Viewport), "Absent"),
            Fact("Open Graph · titre", seo.OpenGraphTitle, !string.IsNullOrWhiteSpace(seo.OpenGraphTitle)),
            Fact("Open Graph · description", seo.OpenGraphDescription, !string.IsNullOrWhiteSpace(seo.OpenGraphDescription)),
            Fact("Open Graph · image", seo.OpenGraphImage, !string.IsNullOrWhiteSpace(seo.OpenGraphImage)),
            Fact("Twitter Card", seo.TwitterCard, !string.IsNullOrWhiteSpace(seo.TwitterCard)),
            Fact("Titres", $"{seo.H1Count} H1 · {seo.HeadingCount} titres au total", seo.H1Count == 1, "Aucun titre détecté"),
            Fact("Images", imageStatus, seo.ImageCount > 0 && seo.ImagesMissingAlt == 0, "Aucune image"),
            Fact("Liens", $"{seo.InternalLinkCount} internes · {seo.ExternalLinkCount} externes · {seo.LinkCount} au total", seo.LinkCount > 0, "Aucun lien détecté"),
            Fact("Données structurées JSON-LD", structured, !string.IsNullOrWhiteSpace(structured), "Aucun type JSON-LD repéré")
        ];
    }

    private static string TextOr(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private async Task<bool> RefreshBrowserAndWaitAsync()
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _browserRefreshCompletion = completion;
        Browser.Refresh();
        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(12));
        }
        catch (TimeoutException)
        {
            return false;
        }
        finally
        {
            if (ReferenceEquals(_browserRefreshCompletion, completion))
                _browserRefreshCompletion = null;
        }
    }

    private async void AriaTreeNode_PointerEntered(object? sender, RoutedEventArgs e) => await HighlightAriaNodeAsync(sender);
    private async void AriaTreeNode_GotFocus(object? sender, RoutedEventArgs e) => await HighlightAriaNodeAsync(sender);
    private async void AriaTreeNode_PointerExited(object? sender, RoutedEventArgs e) => await ClearAriaHighlightAsync();
    private async void AriaTreeNode_LostFocus(object? sender, RoutedEventArgs e) => await ClearAriaHighlightAsync();

    private async void AriaTreeNode_Click(object? sender, RoutedEventArgs e) => await HighlightAriaNodeAsync(sender);

    private async Task HighlightAriaNodeAsync(object? sender)
    {
        if (sender is not Button { Tag: string nodeId } || !nodeId.StartsWith("aria-", StringComparison.Ordinal) ||
            !int.TryParse(nodeId.AsSpan(5), out _))
            return;
        var node = AriaTreeNodes.FirstOrDefault(item => item.Id == nodeId);
        if (node is null)
            return;
        SelectionSummary.Text = $"{node.Badge} · {node.Name}{node.State}";
        try { await Browser.InvokeScript($"window.__rgaaStudioHighlightAriaNode('{nodeId}')"); }
        catch (Exception) { }
    }

    private async Task ClearAriaHighlightAsync()
    {
        if (_pageViewMode != "aria")
            return;
        try { await Browser.InvokeScript("window.__rgaaStudioClearAriaHighlight()"); }
        catch (Exception) { }
    }

    private static string? DecodeScriptString(string? result)
    {
        if (string.IsNullOrEmpty(result) || result[0] != '"')
            return result;
        try { return JsonSerializer.Deserialize<string>(result); }
        catch (JsonException) { return result; }
    }

    private async Task ShowMediaPreviewAsync(string tag, string source, string previewDataUrl)
    {
        Bitmap? previewBitmap = null;
        const string dataPrefix = "data:image/png;base64,";
        if (previewDataUrl.StartsWith(dataPrefix, StringComparison.Ordinal) && previewDataUrl.Length <= 800_000)
        {
            try
            {
                var previewBytes = Convert.FromBase64String(previewDataUrl[dataPrefix.Length..]);
                if (previewBytes.Length <= 600_000)
                {
                    using var stream = new MemoryStream(previewBytes, writable: false);
                    var candidate = new Bitmap(stream);
                    if (candidate.PixelSize.Width is > 0 and <= 512 && candidate.PixelSize.Height is > 0 and <= 320)
                        previewBitmap = candidate;
                    else
                        candidate.Dispose();
                }
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidDataException or NotSupportedException) { }
        }

        var closeButton = new Button
        {
            Width = 36,
            Height = 36,
            Padding = new Thickness(0),
            Background = Brush.Parse("#FFFDF7"),
            BorderBrush = Brush.Parse("#7B8190"),
            BorderThickness = new Thickness(1),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Content = new Avalonia.Controls.Shapes.Path
            {
                Data = Geometry.Parse("M 5,5 L 19,19 M 19,5 L 5,19"),
                Stroke = Brush.Parse("#171F3B"),
                StrokeThickness = 2,
                Width = 14,
                Height = 14
            }
        };
        closeButton.SetValue(AutomationProperties.NameProperty, "Fermer l’aperçu de l’élément");
        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,40") };
        headerGrid.Children.Add(new TextBlock
        {
            Text = $"Aperçu de <{tag}>",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#171F3B"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        Grid.SetColumn(closeButton, 1);
        headerGrid.Children.Add(closeButton);
        var header = new Border
        {
            Padding = new Thickness(20, 14),
            Background = Brush.Parse("#FFFDF7"),
            BorderBrush = Brush.Parse("#D8DAD9"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = headerGrid
        };

        Control previewContent = previewBitmap is null
            ? new TextBlock
            {
                Text = tag == "audio"
                    ? "Cet élément audio ne possède pas d’aperçu visuel."
                    : "Aucun aperçu local n’est disponible pour cette ressource (média non chargé ou origine différente).",
                FontSize = 13,
                Foreground = Brush.Parse("#45536D"),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                MaxWidth = 440
            }
            : new Image { Source = previewBitmap, Stretch = Avalonia.Media.Stretch.Uniform, MaxWidth = 512, MaxHeight = 320 };
        var previewBorder = new Border
        {
            Margin = new Thickness(18, 10),
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#D8DAD9"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = previewContent
        };
        var formattedSource = source.Length > 12_000 ? source[..12_000] + "…" : source;
        try { formattedSource = SiteDiscoveryService.FormatHtmlFragment(formattedSource); }
        catch (Exception) { }
        var sourceEditor = new AvaloniaEdit.TextEditor
        {
            Text = formattedSource,
            IsReadOnly = true,
            ShowLineNumbers = true,
            LineNumbersForeground = Brush.Parse("#555E70"),
            WordWrap = false,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = Brush.Parse("#FFFDF7"),
            Foreground = Brush.Parse("#171F3B"),
            FontFamily = "Consolas",
            FontSize = 12
        };
        sourceEditor.SyntaxHighlighting = SourceCodeView.SyntaxHighlighting;
        var dialogBody = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,180") };
        var previewHeading = new TextBlock
        {
            Text = "Prévisualisation",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#171F3B"),
            Margin = new Thickness(20, 14, 20, 0)
        };
        var codeHeading = new TextBlock
        {
            Text = "Code source de l’élément",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#171F3B"),
            Margin = new Thickness(20, 6, 20, 0)
        };
        Grid.SetRow(previewHeading, 0);
        Grid.SetRow(previewBorder, 1);
        Grid.SetRow(codeHeading, 2);
        Grid.SetRow(sourceEditor, 3);
        sourceEditor.Margin = new Thickness(18, 8, 18, 16);
        dialogBody.Children.Add(previewHeading);
        dialogBody.Children.Add(previewBorder);
        dialogBody.Children.Add(codeHeading);
        dialogBody.Children.Add(sourceEditor);

        var dialog = new Window
        {
            Title = "Aperçu du média",
            Width = 640,
            Height = 580,
            MinWidth = 520,
            MinHeight = 480,
            CanResize = true,
            WindowDecorations = WindowDecorations.None,
            ShowInTaskbar = false,
            Background = Brush.Parse("#EEF2FA"),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Margin = new Thickness(12),
                Background = Brush.Parse("#FFFDF7"),
                BorderBrush = Brush.Parse("#D8DAD9"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Children = { header, dialogBody } }
            }
        };
        Grid.SetRow(dialogBody, 1);
        closeButton.Click += (_, _) => dialog.Close(false);
        header.PointerPressed += (_, args) =>
        {
            if (args.GetCurrentPoint(dialog).Properties.IsLeftButtonPressed)
                dialog.BeginMoveDrag(args);
        };
        try { await dialog.ShowDialog<bool>(this); }
        finally { previewBitmap?.Dispose(); }
    }

    private void ShowPageViewError(string message)
    {
        PageViewStatusText.Text = message;
        PageViewStatus.IsVisible = true;
    }

    private void AuditPage_ContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pageId)
        {
            e.Handled = true;
            ShowAuditPageActions(button, pageId);
        }
    }

    private void AuditPageActions_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pageId)
            ShowAuditPageActions(button, pageId);
    }

    private void ShowAuditPageActions(Button target, string pageId)
    {
        if (_activeAudit is null || AuditPages.FirstOrDefault(page => page.Id == pageId) is not { } page)
            return;
        var menu = new MenuFlyout();
        var delete = new MenuItem { Header = "Supprimer la page…" };
        delete.Click += async (_, _) => await ConfirmDeletePageAsync(_activeAudit.Id, page);
        menu.Items.Add(delete);
        menu.ShowAt(target);
    }

    private void LoadLocalFixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Assets", "fixture.html");
        if (!File.Exists(fixturePath))
            return;

        Browser.Source = new Uri(fixturePath);
        BrowserModeLabel.Text = "Fixture locale · démonstration";
        BrowserModeBorder.SetValue(AutomationProperties.NameProperty, "Le navigateur affiche une fixture locale fictive");
        UpdateInspectionMenuCommands();
        Browser.Focus();
    }

    private void Browser_NavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        if (_contrastPipetteActive)
            _ = StopContrastPipetteAsync("Navigation démarrée ; pipette arrêtée.");
        var allowedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Assets", "fixture.html"));
        if (e.Request is not { IsAbsoluteUri: true } target ||
            !(target.IsFile && string.Equals(Path.GetFullPath(target.LocalPath), allowedPath, StringComparison.OrdinalIgnoreCase)) &&
            !IsSelectedAuditPageUrl(target))
            e.Cancel = true;
    }

    private bool IsSelectedAuditPageUrl(Uri target)
    {
        if (_activeAudit is null || target.Scheme != Uri.UriSchemeHttps || !target.IsDefaultPort ||
            !string.IsNullOrEmpty(target.UserInfo) ||
            !Uri.TryCreate(_activeAudit.SiteUrl, UriKind.Absolute, out var site) ||
            !target.Host.Equals(site.Host, StringComparison.OrdinalIgnoreCase))
            return false;

        var targetPath = NormalizeBrowserPath(target);
        return _dossiers.GetPages(_activeAudit.Id).Any(page =>
            Uri.TryCreate(page.Url, UriKind.Absolute, out var allowed) &&
            allowed.Host.Equals(target.Host, StringComparison.OrdinalIgnoreCase) &&
            allowed.Scheme == target.Scheme && allowed.Port == target.Port &&
            string.Equals(NormalizeBrowserPath(allowed), targetPath, StringComparison.Ordinal));
    }

    private static string NormalizeBrowserPath(Uri uri)
    {
        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.Length == 0)
            path = "/";
        return path + uri.Query;
    }

    private async void Browser_NavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        _browserRefreshCompletion?.TrySetResult(e.IsSuccess);
        if (e.IsSuccess && !IsLocalFixture)
        {
            try { _pageSourceSnapshot = await Browser.InvokeScript("document.documentElement.outerHTML"); }
            catch (Exception) { _pageSourceSnapshot = null; }
        }

        if (e.IsSuccess && _inspectionActive)
        {
            try { await Browser.InvokeScript("window.fixtureSetInspect(true)"); }
            catch (Exception) { }
        }
    }

    private async void Browser_WebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.Body) || e.Body.Length > 850_000)
                return;
            using var document = JsonDocument.Parse(e.Body, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
                return;

            if (type.GetString() == "contrast-pick")
            {
                if (!_contrastPipetteActive || e.Body.Length > 8192)
                    return;
                var sample = JsonSerializer.Deserialize<ContrastPipetteSample>(root.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (sample is not null && sample.Selector is { Length: > 0 and <= 2048 } && sample.BackgroundLayers is { Count: <= 40 })
                    await CompleteContrastPickAsync(sample);
                return;
            }
            if (type.GetString() == "contrast-cancel")
            {
                if (_contrastPipetteActive)
                    await StopContrastPipetteAsync("Pipette annulée.");
                return;
            }

            if (type.GetString() == "media-preview")
            {
                if (e.Body.Length > 850_000 || !_siteStylesRemoved || _pageViewMode != "no-css")
                    return;
                var mediaTag = SafeField(root, "tag", 8).ToLowerInvariant();
                if (mediaTag is not ("img" or "video" or "audio"))
                    return;
                var source = SafeField(root, "source", 12_100);
                var preview = SafeField(root, "preview", 810_000);
                await ShowMediaPreviewAsync(mediaTag, source, preview);
                return;
            }

            if (type.GetString() != "selection" || e.Body.Length > 2048)
                return;

            var tag = SafeField(root, "tag", 24).ToUpperInvariant();
            if (tag is not ("INPUT" or "BUTTON" or "A" or "LABEL" or "SELECT" or "TEXTAREA" or "H1" or "P" or "FORM"))
                return;

            var text = SafeField(root, "text", 160);
            var aria = SafeField(root, "aria", 100);
            var state = SafeField(root, "state", 80);
            SelectionSummary.Text = $"{tag}\nTexte visible : {text}\nIndication ARIA : {aria}\nÉtat : {state}";
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
    }

    private void RestoreReviewRecord()
    {
        RuleBox.Text = string.Empty;
        DecisionCombo.SelectedIndex = 0;
        JustificationBox.Text = string.Empty;
        CompleteReviewButton.IsEnabled = false;

        if (_activeAudit is null)
            return;

        var latest = _dossiers.ReadDecisions(_activeAudit.Id).LastOrDefault();
        if (latest is null)
        {
            ReviewStatus.Text = "Aucune décision n’a encore été enregistrée pour ce dossier.";
            return;
        }

        RuleBox.Text = latest.RuleSuggestion;
        JustificationBox.Text = latest.Justification;
        for (var index = 0; index < DecisionCombo.Items.Count; index++)
        {
            if ((DecisionCombo.Items[index] as ComboBoxItem)?.Content?.ToString() == latest.HumanDecision)
            {
                DecisionCombo.SelectedIndex = index;
                break;
            }
        }

        CompleteReviewButton.IsEnabled = !latest.ReviewComplete;
        ReviewStatus.Text = latest.ReviewComplete
            ? "Décision humaine retrouvée ; la revue a été explicitement terminée. Cela ne signifie pas que le site est conforme."
            : "Décision humaine retrouvée ; la revue reste à terminer.";
    }

    private void SaveReview_Click(object? sender, RoutedEventArgs e)
    {
        if (_activeAudit is null)
            return;
        if (_activeAuditPageId is null || _dossiers.GetPages(_activeAudit.Id).FirstOrDefault(page => page.Id == _activeAuditPageId) is not { } activePage)
        {
            ReviewStatus.Text = "Ouvrez d’abord une page du dossier pour rattacher la décision au bon contexte.";
            return;
        }

        var decision = (DecisionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        try
        {
            var record = _dossiers.AppendDecision(
                _activeAudit.Id,
                activePage.Name,
                RuleBox.Text ?? string.Empty,
                decision,
                JustificationBox.Text);
            _activeAudit = _dossiers.Get(_activeAudit.Id) ?? _activeAudit;
            CompleteReviewButton.IsEnabled = true;
            ReviewStatus.Text = $"Décision humaine enregistrée : {record.HumanDecision}. L’achèvement de la revue reste une étape distincte.";
            RefreshAudits();
            RefreshAuditDashboard();
        }
        catch (ArgumentException ex)
        {
            ReviewStatus.Text = ex.Message;
            if (string.IsNullOrWhiteSpace(RuleBox.Text))
                RuleBox.Focus();
            else if (decision is not ("Conforme" or "Non conforme" or "Non applicable" or "Non vérifiable"))
                DecisionCombo.Focus();
            else
                JustificationBox.Focus();
        }
        catch (IOException)
        {
            ReviewStatus.Text = "La décision n’a pas pu être enregistrée dans le dossier local.";
        }
    }

    private void CompleteReview_Click(object? sender, RoutedEventArgs e)
    {
        if (_activeAudit is null)
            return;

        var latest = _dossiers.ReadDecisions(_activeAudit.Id).LastOrDefault();
        var decision = (DecisionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        if (latest is null || latest.RuleSuggestion != RuleBox.Text?.Trim() || latest.HumanDecision != decision || latest.Justification != JustificationBox.Text?.Trim())
        {
            ReviewStatus.Text = "Enregistrez d’abord la décision affichée avant de terminer cette revue.";
            return;
        }

        try
        {
            _dossiers.CompleteLatestReview(_activeAudit.Id);
            _activeAudit = _dossiers.Get(_activeAudit.Id) ?? _activeAudit;
            CompleteReviewButton.IsEnabled = false;
            ReviewStatus.Text = "Revue explicitement terminée par l’auditeur. Cela ne signifie pas que le site est conforme.";
            RefreshAudits();
            RefreshAuditDashboard();
        }
        catch (InvalidOperationException ex)
        {
            ReviewStatus.Text = ex.Message;
        }
        catch (IOException)
        {
            ReviewStatus.Text = "L’état de revue n’a pas pu être enregistré dans le dossier local.";
        }
    }

    private static string SafeField(JsonElement root, string name, int max)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            return string.Empty;
        return new string((value.GetString() ?? string.Empty).Where(character => !char.IsControl(character) || character is '\n' or '\t').Take(max).ToArray());
    }

    private async void SetInspection(bool active)
    {
        if (_inspectionActive == active)
            return;

        _inspectionActive = active;
        InspectBadge.IsVisible = active;
        UpdateInspectionMenuCommands();

        if (Browser.Source is not null)
        {
            try { await Browser.InvokeScript($"window.fixtureSetInspect({active.ToString().ToLowerInvariant()})"); }
            catch (InvalidOperationException) { }
        }

        if (active && WorkspaceView.IsVisible)
            Browser.Focus();
    }

    private void StartInspect_Click(object? sender, RoutedEventArgs e)
    {
        if (!CanInspect)
            return;
        SetInspection(true);
        OptionsPopup.IsOpen = false;
    }

    private void StopInspect_Click(object? sender, RoutedEventArgs e)
    {
        SetInspection(false);
        OptionsPopup.IsOpen = false;
    }

    private void Projects_Click(object? sender, RoutedEventArgs e)
    {
        OptionsPopup.IsOpen = false;
        ShowHome();
    }

    private void ShowHome()
    {
        if (_contrastPipetteActive)
            _ = StopContrastPipetteAsync("Espace d’audit fermé ; pipette arrêtée.");
        RefreshAudits();
        SetupView.IsVisible = false;
        WorkspaceView.IsVisible = false;
        HomeView.IsVisible = true;
        UpdateHomeLayout();
        UpdateInspectionMenuCommands();
        UpdateProjectListState();
    }

    private void CloseAuditTab_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not string id)
            return;

        var index = OpenAuditTabs.ToList().FindIndex(tab => tab.Id == id);
        if (index < 0)
            return;

        var wasActive = _activeAudit?.Id == id;
        var workspaceWasVisible = WorkspaceView.IsVisible;
        OpenAuditTabs.RemoveAt(index);
        if (!wasActive)
            return;

        // Closing a tab never deletes or changes its audit dossier.
        SetInspection(false);
        if (OpenAuditTabs.Count == 0)
        {
            _activeAudit = null;
            if (workspaceWasVisible)
                ShowHome();
            return;
        }

        var nextIndex = Math.Min(index, OpenAuditTabs.Count - 1);
        var next = _dossiers.Get(OpenAuditTabs[nextIndex].Id);
        if (next is null)
        {
            if (workspaceWasVisible)
                ShowHome();
            return;
        }

        if (workspaceWasVisible)
            OpenAudit(next);
        else
        {
            _activeAudit = next;
            AddOrActivateTab(next);
        }
    }

    private void TabActivate_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not string id)
            return;

        var manifest = _dossiers.Get(id);
        if (manifest is not null)
            OpenAudit(manifest);
    }

    private void AddOrActivateTab(AuditManifest manifest)
    {
        for (var index = 0; index < OpenAuditTabs.Count; index++)
            OpenAuditTabs[index] = OpenAuditTabs[index] with { IsActive = OpenAuditTabs[index].Id == manifest.Id, WindowIsActive = _windowIsActive };

        var tabIndex = OpenAuditTabs.ToList().FindIndex(tab => tab.Id == manifest.Id);
        var title = $"{new Uri(manifest.SiteUrl).Host} · {manifest.Version}{(string.IsNullOrEmpty(manifest.Level) ? string.Empty : $" · {manifest.Level}")}";
        var tab = new OpenAuditTab(manifest.Id, title, true, _windowIsActive, Favicon: LoadFavicon(manifest.Id));
        if (tabIndex < 0)
            OpenAuditTabs.Add(tab);
        else
            OpenAuditTabs[tabIndex] = tab;
        RecalculateTabWidths();
    }

    private void TabStrip_SizeChanged(object? sender, SizeChangedEventArgs e) => RecalculateTabWidths(e.NewSize.Width);

    private void RecalculateTabWidths(double? availableWidth = null)
    {
        if (TabStrip is null || OpenTabs is null || TabsScrollViewer is null)
            return;

        var width = availableWidth ?? TabStrip.Bounds.Width;
        if (width <= 0)
            return;
        if (OpenAuditTabs.Count == 0)
        {
            TabsPreviousButton.IsVisible = false;
            TabsNextButton.IsVisible = false;
            TabsPreviousButton.IsEnabled = false;
            TabsNextButton.IsEnabled = false;
            TabsScrollViewer.Offset = new Vector(0, 0);
            return;
        }

        var count = OpenAuditTabs.Count;
        var layout = TabStripLayoutCalculator.Calculate(count, width);

        for (var index = 0; index < count; index++)
        {
            if (Math.Abs(OpenAuditTabs[index].TabWidth - layout.TabWidth) > 0.5)
                OpenAuditTabs[index] = OpenAuditTabs[index] with { TabWidth = layout.TabWidth };
        }

        TabsPreviousButton.IsVisible = layout.NavigationRequired;
        TabsNextButton.IsVisible = layout.NavigationRequired;
        if (!layout.NavigationRequired)
            TabsScrollViewer.Offset = new Vector(0, 0);

        Dispatcher.UIThread.Post(ClampTabScrollOffsetAndUpdate, DispatcherPriority.Loaded);
    }

    private void ScheduleTabLayoutUpdate()
    {
        if (_tabLayoutUpdateScheduled)
            return;

        _tabLayoutUpdateScheduled = true;
        Dispatcher.UIThread.Post(() =>
        {
            _tabLayoutUpdateScheduled = false;
            RecalculateTabWidths();
        }, DispatcherPriority.Loaded);
    }

    private void EnsureOpenTabVisible(string id)
    {
        var index = OpenAuditTabs.ToList().FindIndex(tab => tab.Id == id);
        if (index < 0)
            return;

        var leading = OpenAuditTabs.Take(index).Sum(tab => tab.TabWidth + 8);
        var trailing = leading + OpenAuditTabs[index].TabWidth + 8;
        var offset = TabsScrollViewer.Offset.X;
        var viewport = TabsScrollViewer.Viewport.Width;
        if (leading < offset)
            offset = leading;
        else if (trailing > offset + viewport)
            offset = trailing - viewport;

        var maximum = Math.Max(0, TabsScrollViewer.Extent.Width - viewport);
        TabsScrollViewer.Offset = new Vector(Math.Clamp(offset, 0, maximum), 0);
        UpdateTabNavigationButtons();
    }

    private void UpdateTabNavigationButtons()
    {
        if (TabsPreviousButton is null || TabsNextButton is null || TabsScrollViewer is null)
            return;

        var maxOffset = Math.Max(0, TabsScrollViewer.Extent.Width - TabsScrollViewer.Viewport.Width);
        TabsPreviousButton.IsEnabled = TabsPreviousButton.IsVisible && TabsScrollViewer.Offset.X > 1;
        TabsNextButton.IsEnabled = TabsNextButton.IsVisible && TabsScrollViewer.Offset.X < maxOffset - 1;
    }

    private void ClampTabScrollOffsetAndUpdate()
    {
        var maxOffset = Math.Max(0, TabsScrollViewer.Extent.Width - TabsScrollViewer.Viewport.Width);
        if (TabsScrollViewer.Offset.X > maxOffset)
            TabsScrollViewer.Offset = new Vector(maxOffset, 0);
        UpdateTabNavigationButtons();
    }

    private void ScrollTabs(int direction)
    {
        var maxOffset = Math.Max(0, TabsScrollViewer.Extent.Width - TabsScrollViewer.Viewport.Width);
        var distance = Math.Max(120, TabsScrollViewer.Viewport.Width * 0.75);
        TabsScrollViewer.Offset = new Vector(Math.Clamp(TabsScrollViewer.Offset.X + direction * distance, 0, maxOffset), 0);
        UpdateTabNavigationButtons();
    }

    private void TabsPrevious_Click(object? sender, RoutedEventArgs e) => ScrollTabs(-1);
    private void TabsNext_Click(object? sender, RoutedEventArgs e) => ScrollTabs(1);
    private void TabsScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e) => UpdateTabNavigationButtons();

    private void UpdateProjectListState()
    {
        ProjectsButton.Tag = HomeView.IsVisible;
        ProjectsButton.SetValue(AutomationProperties.NameProperty, HomeView.IsVisible ? "Projets — vue active" : "Projets");
    }

    private void Options_Click(object? sender, RoutedEventArgs e)
    {
        OptionsPopup.IsOpen = !OptionsPopup.IsOpen;
        UpdateOptionsAppearance();
    }

    private void OptionsPopup_Closed(object? sender, EventArgs e)
    {
        UpdateOptionsAppearance();
        OptionsButton.Focus();
    }

    private void UpdateOptionsAppearance()
    {
        var isOpen = OptionsPopup.IsOpen;
        OptionsButton.Tag = isOpen;
        BurgerGlyph.CurrentColor = isOpen ? Colors.White : Color.Parse("#171F3B");
    }

    private void UpdateWindowActivation(bool active)
    {
        _windowIsActive = active;
        TopToolbar.Background = active ? CreateToolbarGradient() : Brush.Parse("#E4E3DD");
        TopToolbar.BorderBrush = Brush.Parse(active ? "#E9DBFF" : "#D2D4D6");

        for (var index = 0; index < OpenAuditTabs.Count; index++)
            OpenAuditTabs[index] = OpenAuditTabs[index] with { WindowIsActive = active };
    }

    private static LinearGradientBrush CreateToolbarGradient() => new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.Parse("#F4EEFF"), 0),
            new GradientStop(Color.Parse("#F7EEF6"), 0.7417),
            new GradientStop(Color.Parse("#FDEDE6"), 1)
        }
    };

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_contrastPipetteActive && e.Key == Key.Escape)
        {
            _ = StopContrastPipetteAsync("Pipette annulée.");
            e.Handled = true;
            return;
        }

        if (!OptionsPopup.IsOpen && FocusManager?.GetFocusedElement() == OptionsButton && e.Key is Key.Enter or Key.Space)
        {
            OptionsPopup.IsOpen = true;
            UpdateOptionsAppearance();
            FocusFirstEnabledMenuItem();
            e.Handled = true;
            return;
        }

        if (OptionsPopup.IsOpen)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    OptionsPopup.IsOpen = false;
                    e.Handled = true;
                    return;
                case Key.Down:
                    FocusMenuItem(1);
                    e.Handled = true;
                    return;
                case Key.Up:
                    FocusMenuItem(-1);
                    e.Handled = true;
                    return;
                case Key.Enter:
                case Key.Space:
                    if (FocusManager?.GetFocusedElement() is Button focused && focused.IsEnabled)
                    {
                        focused.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        e.Handled = true;
                        return;
                    }
                    break;
                case Key.F6:
                    if (!_inspectionActive && CanInspect)
                    {
                        SetInspection(true);
                        OptionsPopup.IsOpen = false;
                    }
                    else if (_inspectionActive && WorkspaceView.IsVisible)
                    {
                        _ = SelectFocusedFixtureElementAsync();
                    }
                    e.Handled = true;
                    return;
            }
        }
        else if (e.Key == Key.Escape && _inspectionActive)
        {
            SetInspection(false);
            e.Handled = true;
        }
        else if (e.Key == Key.F6 && !_inspectionActive && CanInspect)
        {
            SetInspection(true);
            e.Handled = true;
        }
        else if (e.Key == Key.F6 && _inspectionActive && WorkspaceView.IsVisible)
        {
            _ = SelectFocusedFixtureElementAsync();
            e.Handled = true;
        }
    }

    private async Task SelectFocusedFixtureElementAsync()
    {
        if (Browser.Source is not null)
        {
            try { await Browser.InvokeScript("window.fixtureSelectFocused()"); }
            catch (InvalidOperationException) { }
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        const double resizeGrip = 6;
        var point = e.GetPosition(this);
        var left = point.X <= resizeGrip;
        var right = point.X >= Bounds.Width - resizeGrip;
        var top = point.Y <= resizeGrip;
        var bottom = point.Y >= Bounds.Height - resizeGrip;
        var edge = (left, right, top, bottom) switch
        {
            (true, _, true, _) => WindowEdge.NorthWest,
            (_, true, true, _) => WindowEdge.NorthEast,
            (true, _, _, true) => WindowEdge.SouthWest,
            (_, true, _, true) => WindowEdge.SouthEast,
            (true, _, _, _) => WindowEdge.West,
            (_, true, _, _) => WindowEdge.East,
            (_, _, true, _) => WindowEdge.North,
            (_, _, _, true) => WindowEdge.South,
            _ => (WindowEdge?)null
        };

        if (edge is { } resizeEdge)
            BeginResizeDrag(resizeEdge, e);
    }

    private void FocusFirstEnabledMenuItem()
    {
        var first = _inspectionActive
            ? StopInspectItem
            : MenuItems.GetVisualDescendants().OfType<Button>().FirstOrDefault(button => button.IsEnabled && button.IsVisible);
        first?.Focus();
    }

    private void FocusMenuItem(int direction)
    {
        var items = MenuItems.GetVisualDescendants().OfType<Button>().Where(button => button.IsEnabled && button.IsVisible).ToArray();
        if (items.Length == 0)
            return;
        var current = Array.IndexOf(items, FocusManager?.GetFocusedElement() as Button);
        var next = current < 0 ? 0 : (current + direction + items.Length) % items.Length;
        items[next].Focus();
    }

    private void UpdateInspectionMenuCommands()
    {
        StartInspectItem.IsVisible = !_inspectionActive;
        StartInspectItem.IsEnabled = !_inspectionActive && CanInspect;
        StopInspectItem.IsEnabled = _inspectionActive;
        StartInspectItem.SetValue(AutomationProperties.HelpTextProperty, _inspectionActive ? "Masqué pendant l’inspection active." : "Démarre l’inspection de la fixture locale.");
        StopInspectItem.SetValue(AutomationProperties.HelpTextProperty, _inspectionActive ? "Termine l’inspection active." : "Indisponible : aucune inspection n’est active.");
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        OptionsPopup.IsOpen = false;
        var dataDirectory = Path.GetDirectoryName(_dossiers.DatabasePath)!;
        var settings = new SettingsWindow(_userPreferences, dataDirectory, SaveUserPreferences);
        await settings.ShowDialog<DesktopUserPreferences?>(this);
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        OptionsPopup.IsOpen = false;
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString() ?? "Version inconnue";
        var about = new AboutWindow(version, Path.Combine(AppContext.BaseDirectory, "LICENSE.txt"));
        await about.ShowDialog(this);
    }

    private DesktopUserPreferences LoadUserPreferences()
    {
        var settingsPath = Path.Combine(Path.GetDirectoryName(_dossiers.DatabasePath)!, "settings.json");
        if (!File.Exists(settingsPath)) return new DesktopUserPreferences();
        try
        {
            return JsonSerializer.Deserialize<DesktopUserPreferences>(File.ReadAllText(settingsPath)) ?? new DesktopUserPreferences();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return new DesktopUserPreferences();
        }
    }

    private bool SaveUserPreferences(DesktopUserPreferences preferences)
    {
        var settingsPath = Path.Combine(Path.GetDirectoryName(_dossiers.DatabasePath)!, "settings.json");
        var temporaryPath = settingsPath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, settingsPath, overwrite: true);
            _userPreferences = preferences;
            _sidePanelShown = preferences.ShowInspectionPanel;
            _pageScopeCollapsed = preferences.CollapsePageScope;
            UpdateWorkspaceLayout();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            return false;
        }
    }

    private void PersistCurrentUserPreferences() => SaveUserPreferences(_userPreferences with
    {
        ShowInspectionPanel = _sidePanelShown,
        CollapsePageScope = _pageScopeCollapsed
    });

    private void Quit_Click(object? sender, RoutedEventArgs e) => Close();
    private void ToggleSidePanel_Click(object? sender, RoutedEventArgs e)
    {
        _sidePanelShown = !_sidePanelShown;
        PersistCurrentUserPreferences();
        SidePanel.IsVisible = _sidePanelShown;
        UpdateWorkspaceLayout();
    }
    private void Minimize_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    public sealed record AuditListItem(string Id, string Title, string Summary, string UpdatedLabel, bool IsArchived, Bitmap? Favicon = null)
    {
        public bool HasFavicon => Favicon is not null;
        public bool ShowGlobe => Favicon is null;
        public string AccessibleName => $"Ouvrir l’audit de {Title}, {Summary}, {UpdatedLabel}{(IsArchived ? ", archivé" : string.Empty)}.";
        public string ActionsAccessibleName => $"Actions pour l’audit de {Title}";
    }

    public sealed class SitePageChoice(SitePageCandidate candidate)
    {
        public SitePageCandidate Candidate { get; } = candidate;
        public string Name => Candidate.Name;
        public string Url => Candidate.Url;
        public string Category => Candidate.Category;
        public bool IsSelected { get; set; }
    }

    public sealed record AriaTreeNode(string Id, string Badge, string Role, string Name, bool MissingName, int Depth, bool IsLink, string State)
    {
        public Thickness Indent => new(Math.Min(Depth, 12) * 12, 0, 0, 3);
        public bool HasState => !string.IsNullOrWhiteSpace(State);
        public bool HasName => !string.IsNullOrWhiteSpace(Name) || MissingName;
        public string DisplayName => MissingName ? "Nom accessible manquant" : Name;
        public string AccessibleName => $"{Badge}, {DisplayName}{State}";
        public IBrush NameForeground => MissingName ? Brush.Parse("#8B2D24") : Brush.Parse("#171F3B");
        public IBrush BadgeBackground => MissingName ? Brush.Parse("#FBEDEA") : Badge.StartsWith('H') ? Brush.Parse("#FFF4D9") : Role switch
        {
            "link" => Brush.Parse("#E6F6F2"),
            "navigation" or "banner" or "main" or "contentinfo" or "complementary" or "region" => Brush.Parse("#E9EFF9"),
            _ => Brush.Parse("#F3F4F6")
        };
        public IBrush BadgeForeground => MissingName ? Brush.Parse("#8B2D24") : Badge.StartsWith('H') ? Brush.Parse("#6A5016") : Role switch
        {
            "link" => Brush.Parse("#175F59"),
            "navigation" or "banner" or "main" or "contentinfo" or "complementary" or "region" => Brush.Parse("#214B9A"),
            _ => Brush.Parse("#45536D")
        };
    }

    public sealed record AxeResultItem(string Id, string Heading, string Summary, string ReviewStatus);
    public sealed record AxeElementContext(bool Missing, string Tag, string Text, string Html, double X, double Y,
        double Width, double Height, double ViewportWidth, double ViewportHeight, List<string> Technologies);
    public sealed record AxeElementRect(double X, double Y, double Width, double Height, double ViewportWidth, double ViewportHeight);
    public sealed record ContrastPipetteSample(string Type, string Tag, string Selector, string Text, string Foreground,
        List<string>? BackgroundLayers, string BackgroundImages, string OpacityLayers, string BorderColor, bool HasBorder,
        string FontSize, string FontWeight);
    private sealed record ContrastReviewContext(ContrastPipetteSample Sample, ColorContrastMeasurement? Measurement,
        double RequiredRatio, bool IsText, bool IsGraphicalObject, bool HasUncertainBackground);
    public sealed record InspectionEvidenceItem(string Id, string Title, string Description);
    public sealed record TechnologyFinding(string Name, string Category, string Evidence, string Confidence, string Version)
    {
        public bool HasVersion => !string.IsNullOrWhiteSpace(Version);
        public string VersionSummary => $"Version détectée : {Version}";
    }
    public sealed record TechnologyPageProfile(string Title, string Url, string Language, string Html, List<string> Globals,
        int ScriptCount, int ExternalScriptCount, int StylesheetCount, int ImageCount, int FormCount, SeoPageSnapshot? Seo);
    public sealed record SeoPageSnapshot
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Canonical { get; init; } = string.Empty;
        public string Robots { get; init; } = string.Empty;
        public string Language { get; init; } = string.Empty;
        public string Charset { get; init; } = string.Empty;
        public string Viewport { get; init; } = string.Empty;
        public string OpenGraphTitle { get; init; } = string.Empty;
        public string OpenGraphDescription { get; init; } = string.Empty;
        public string OpenGraphImage { get; init; } = string.Empty;
        public string TwitterCard { get; init; } = string.Empty;
        public int H1Count { get; init; }
        public int HeadingCount { get; init; }
        public int ImageCount { get; init; }
        public int ImagesMissingAlt { get; init; }
        public int LinkCount { get; init; }
        public int InternalLinkCount { get; init; }
        public int ExternalLinkCount { get; init; }
        public List<string> StructuredDataTypes { get; init; } = [];
    }
    public sealed record SeoFinding(string Label, string Value, string State)
    {
        public IBrush StateBackground => State switch
        {
            "Présent" => Brush.Parse("#E6F6F2"),
            "À vérifier" => Brush.Parse("#FFF4D9"),
            _ => Brush.Parse("#F3F4F6")
        };
        public IBrush StateForeground => State switch
        {
            "Présent" => Brush.Parse("#175F59"),
            "À vérifier" => Brush.Parse("#6A5016"),
            _ => Brush.Parse("#555E70")
        };
    }

    public sealed record SemanticTreeNode(string Id, string Badge, string Tag, string Summary, int Depth, string Locator, string RuleId, string Issue, bool Advisory, bool IsAddedToReport = false, bool IsPageSelected = false)
    {
        public Thickness Indent => new(Math.Min(Depth, 10) * 12, 0, 0, 3);
        public bool HasIssue => !string.IsNullOrWhiteSpace(RuleId);
        public bool CanAddToReport => HasIssue && IsPageSelected && !IsAddedToReport;
        public string ReportActionLabel => IsAddedToReport ? "Ajouté au rapport" : IsPageSelected ? "Ajouter au rapport" : "Ouvrir une page du dossier";
        public string IssueLabel => RuleId switch
        {
            var id when id.StartsWith("media.img.alt-missing", StringComparison.Ordinal) => "Alt manquant",
            var id when id.StartsWith("media.img.link-name", StringComparison.Ordinal) => "Lien sans nom",
            var id when id.StartsWith("media.video.captions", StringComparison.Ordinal) => "Sous-titres à vérifier",
            var id when id.StartsWith("media.audio.transcript", StringComparison.Ordinal) => "Transcription à vérifier",
            var id when id.StartsWith("media.svg.name", StringComparison.Ordinal) => "Nom accessible à vérifier",
            var id when id.StartsWith("media.canvas.fallback", StringComparison.Ordinal) => "Alternative de repli",
            _ when Advisory => "À vérifier",
            _ => "Balise mal placée"
        };
        public IBrush BadgeBackground => Advisory ? Brush.Parse("#FFF4D9") : HasIssue ? Brush.Parse("#FBEDEA") : Brush.Parse("#E9EFF9");
        public IBrush BadgeForeground => Advisory ? Brush.Parse("#6A5016") : HasIssue ? Brush.Parse("#8B2D24") : Brush.Parse("#214B9A");
        public IBrush IssueForeground => Advisory ? Brush.Parse("#6A5016") : Brush.Parse("#8B2D24");
        public string AccessibleName => $"{Badge} {Summary}{(HasIssue ? $". {IssueLabel}: {Issue}" : string.Empty)}";
    }

    public sealed record AuditDashboardMetric(string Label, string Value, string Detail);

    public sealed record AuditPageListItem(string Id, string Name, string DisplayPath, string SamplingReason,
        bool IsCurrentPage, bool IsOpened, int RecordedTestCount, int PendingSuggestionCount)
    {
        public string ProgressSummary => !IsOpened ? "À consulter" : RecordedTestCount > 0
            ? $"{RecordedTestCount} contrôle(s) consigné(s)" : "Ouverte · contrôles à consigner";
        public bool HasPendingSuggestions => PendingSuggestionCount > 0;
        public string SuggestionSummary => $"{PendingSuggestionCount} suggestion(s) à valider";
        public string AccessibleName => $"Ouvrir la page {Name}, {DisplayPath}, {ProgressSummary}{(PendingSuggestionCount > 0 ? $", {PendingSuggestionCount} suggestions à vérifier" : string.Empty)}{(IsCurrentPage ? ", actuellement affichée dans le navigateur" : string.Empty)}";
        public string ActionsAccessibleName => $"Actions pour la page {Name}";
    }

    public sealed record OpenAuditTab(string Id, string Title, bool IsActive, bool WindowIsActive, double TabWidth = 230, Bitmap? Favicon = null)
    {
        public bool HasFavicon => Favicon is not null;
        public bool ShowGlobe => Favicon is null;
        public string VisualState => (IsActive, WindowIsActive) switch
        {
            (true, true) => "Active",
            (true, false) => "ActiveWindowInactive",
            (false, false) => "InactiveWindowInactive",
            _ => "Inactive"
        };
        public string Status => IsActive ? "Dossier actif" : "Dossier ouvert";
        public string CloseName => $"Fermer le dossier {Title}";
    }
}
