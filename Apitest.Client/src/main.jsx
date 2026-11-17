import React, { useState } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'

const apiUrl = import.meta.env.VITE_API_URL ?? 'https://localhost:7086'
const sessionKey = 'apitest-session'

function App() {
  const [session, setSession] = useState(() => JSON.parse(sessionStorage.getItem(sessionKey)))
  const [login, setLogin] = useState('')
  const [password, setPassword] = useState('')
  const [productId, setProductId] = useState('')
  const [reservationId, setReservationId] = useState('')
  const [result, setResult] = useState(null)
  const [busy, setBusy] = useState(false)

  function saveSession(value) {
    setSession(value)
    if (value) sessionStorage.setItem(sessionKey, JSON.stringify(value))
    else sessionStorage.removeItem(sessionKey)
  }

  async function request(path, { method = 'GET', body, authenticated = true, success } = {}) {
    setBusy(true)
    try {
      const headers = {}
      if (body) headers['Content-Type'] = 'application/json'
      if (authenticated && session) headers.Authorization = `Bearer ${session.accessToken}`
      const response = await fetch(`${apiUrl}${path}`, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined,
      })
      const data = response.headers.get('content-type')?.includes('json') ? await response.json() : null
      const messages = {
        401: 'Неверные данные входа или отсутствует действительный токен.',
        403: 'Недостаточно прав для этой операции.',
        429: 'Слишком много попыток входа. Подождите минуту.',
      }
      setResult({
        ok: response.ok,
        status: response.status,
        message: response.ok ? success : data?.title ?? messages[response.status] ?? 'Ошибка запроса.',
      })
      return response.ok ? data : null
    } catch {
      setResult({
        ok: false,
        message: 'Браузер не получил ответ. Проверьте, запущен ли API и разрешён ли адрес клиента в CORS. Причина указана в консоли браузера.',
      })
      return null
    } finally {
      setBusy(false)
    }
  }

  async function authenticate(action) {
    const data = await request(`/api/auth/${action}`, {
      method: 'POST',
      body: { login, password },
      authenticated: false,
      success: action === 'register' ? 'Учётная запись создана.' : 'Вход выполнен.',
    })
    if (data) {
      saveSession({ ...data, login: login.trim() })
      setPassword('')
    }
  }

  async function createReservation(event) {
    event.preventDefault()
    const reservation = await request('/api/productreservations', {
      method: 'POST',
      body: { productId: Number(productId), userId: session.userId, quantity: 1 },
      success: 'Резерв создан.',
    })
    if (reservation) setReservationId(String(reservation.productReservationId))
  }

  return (
    <main>
      <header><h1>Резервы товаров</h1><p>Практическая работа № 3 · Apitest</p></header>
      <section>
        <h2>Учётная запись</h2>
        {session ? (
          <div className="session">
            <p><strong>{session.login}</strong> · {session.role} · ID {session.userId}</p>
            <button type="button" className="secondary" onClick={() => saveSession(null)}>Выйти</button>
          </div>
        ) : (
          <form onSubmit={(event) => {
            event.preventDefault()
            authenticate(event.nativeEvent.submitter?.value ?? 'login')
          }}>
            <label>Логин<input autoComplete="username" required maxLength={100} value={login} onChange={event => setLogin(event.target.value)} /></label>
            <label>Пароль<input autoComplete="current-password" type="password" required maxLength={128} value={password} onChange={event => setPassword(event.target.value)} /></label>
            <div className="actions">
              <button disabled={busy} value="login">Войти</button>
              <button disabled={busy} value="register" className="secondary">Зарегистрироваться</button>
            </div>
          </form>
        )}
      </section>
      <section>
        <h2>Проверка доступа</h2>
        {session && (
          <form className="operation" onSubmit={createReservation}>
            <label>ID товара<input type="number" min="1" required value={productId} onChange={event => setProductId(event.target.value)} /></label>
            <button disabled={busy}>Создать резерв на 1 шт.</button>
          </form>
        )}
        <form className="operation" onSubmit={(event) => {
          event.preventDefault()
          request(`/api/productreservations/${reservationId}/cancel`, { method: 'POST', success: 'Резерв отменён.' })
        }}>
          <label>ID резерва<input type="number" min="1" required value={reservationId} onChange={event => setReservationId(event.target.value)} /></label>
          <button disabled={busy}>Отменить резерв</button>
        </form>
        <button type="button" className="secondary" disabled={busy} onClick={() =>
          request('/api/productreservations', { success: 'Список резервов доступен администратору.' })
        }>Проверить доступ к общему списку</button>
      </section>
      <div className="result" aria-live="polite">
        {result && <p className={result.ok ? 'success' : 'error'}>{result.status && <strong>HTTP {result.status} · </strong>}{result.message}</p>}
      </div>
      <footer>{window.location.origin} → {apiUrl}</footer>
    </main>
  )
}

createRoot(document.getElementById('root')).render(<App />)
