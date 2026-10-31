'use strict';

const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('spike', Object.freeze({
  navigate: url => ipcRenderer.invoke('navigate', url),
  inspect: () => ipcRenderer.invoke('inspect'),
  focusTarget: () => ipcRenderer.invoke('focus-target'),
  focusAudit: () => ipcRenderer.invoke('focus-audit'),
  listAudits: () => ipcRenderer.invoke('audit-list'),
  createAudit: title => ipcRenderer.invoke('audit-create', title),
  openAudit: id => ipcRenderer.invoke('audit-open', id),
  saveResult: (id, outcome, justification) => ipcRenderer.invoke('audit-save-result', id, outcome, justification),
  setObservation: (id, disposition) => ipcRenderer.invoke('audit-observation', id, disposition),
  completeAudit: id => ipcRenderer.invoke('audit-complete', id),
  onStatus: callback => {
    if (typeof callback !== 'function') return;
    const listener = (_event, payload) => callback(payload);
    ipcRenderer.on('status', listener);
    return () => ipcRenderer.removeListener('status', listener);
  }
}));
