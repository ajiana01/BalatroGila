// BalatroUI/src/services/api.js

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5264/api';

// Session ID disimpan di memory / sessionStorage agar terisolasi per tab browser
let sessionId = null;

export function getSessionId() {
  if (!sessionId) {
    sessionId = sessionStorage.getItem('balatro_session_id') || null;
  }
  return sessionId;
}

export function setSessionId(id) {
  sessionId = id;
  if (id) {
    sessionStorage.setItem('balatro_session_id', id);
  } else {
    sessionStorage.removeItem('balatro_session_id');
  }
}

// Fungsi: generateSessionId
// Buat unique session ID
export function generateSessionId() {
  const newId = `session-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  setSessionId(newId);
  return newId;
}

// Helper utama API request
async function apiRequest(endpoint, method = 'GET', body = null) {
  const currentSessionId = getSessionId();
  const headers = {
    'Content-Type': 'application/json',
  };

  if (currentSessionId) {
    headers['X-Session-Id'] = currentSessionId;
  }

  const config = {
    method,
    headers,
  };

  if (body && method !== 'GET') {
    config.body = JSON.stringify(body);
  }

  let response;
  try {
    response = await fetch(`${API_BASE_URL}${endpoint}`, config);
  } catch (netErr) {
    throw new Error(`Koneksi ke backend gagal (${netErr.message}). Pastikan server .NET aktif di ${API_BASE_URL}`);
  }

  let data;
  try {
    data = await response.json();
  } catch (jsonErr) {
    throw new Error(`Respon tidak valid dari server (${response.status})`);
  }

  if (!response.ok || !data.success) {
    throw new Error(data.message || `API Error: ${response.status}`);
  }

  return data.data;
}

// =========================
// HEALTH & STATUS APIS
// =========================

export async function checkBackendStatus() {
  try {
    const res = await fetch(`${API_BASE_URL.replace(/\/api$/, '')}/api/status`);
    if (!res.ok) return false;
    const json = await res.json();
    return json.message === 'Backend BalatroGila is running!' || Boolean(json.version);
  } catch {
    return false;
  }
}

// =========================
// GAME CONTROLLER APIS
// =========================

export async function startGame(playerName = 'Player 1') {
  const currentSessionId = getSessionId();
  return apiRequest('/game/start', 'POST', {
    playerName,
    sessionId: currentSessionId,
  });
}

export async function getGameState() {
  return apiRequest('/game/state');
}

export async function getAvailableBlinds() {
  return apiRequest('/game/blinds');
}

export async function selectBlind(blindId) {
  return apiRequest('/game/blinds/select', 'POST', { blindId });
}

// =========================
// ACTION CONTROLLER APIS
// =========================

export async function playHand(cardIds) {
  return apiRequest('/action/play-hand', 'POST', { cardIds });
}

export async function discardCards(cardIds) {
  return apiRequest('/action/discard', 'POST', { cardIds });
}

export async function getScorePreview(cardIds) {
  return apiRequest('/action/score-preview', 'POST', { cardIds });
}

export async function useConsumable(consumableId, targetCardIds = []) {
  return apiRequest('/action/use-consumable', 'POST', {
    consumableId,
    targetCardIds,
  });
}

export async function sellCard(cardId) {
  return apiRequest('/action/sell-card', 'POST', { cardId });
}

export async function reorderJokers(jokerIds) {
  return apiRequest('/action/reorder-jokers', 'POST', { jokerIds });
}

// =========================
// SHOP CONTROLLER APIS
// =========================

export async function getShop() {
  return apiRequest('/shop');
}

export async function buyCard(cardId) {
  return apiRequest('/shop/buy-card', 'POST', { cardId });
}

export async function rerollShop() {
  return apiRequest('/shop/reroll', 'POST');
}

export async function buyBooster(boosterId) {
  return apiRequest('/shop/buy-booster', 'POST', { boosterId });
}

export async function selectBoosterCard(cardId) {
  return apiRequest('/shop/select-booster-card', 'POST', { cardId });
}

export async function buyVoucher(voucherId) {
  return apiRequest('/shop/buy-voucher', 'POST', { voucherId });
}

export async function leaveShop() {
  return apiRequest('/shop/leave', 'POST');
}
