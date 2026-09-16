/**
 * Точка входа в приложение.
 */
import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';

const container = document.getElementById('root');

if (!container) {
    throw new Error(
        'Не найден корневой элемент #root. Проверьте index.html.'
    );
}

createRoot(container).render(<App />);