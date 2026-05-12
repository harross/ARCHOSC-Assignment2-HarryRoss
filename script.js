const myName = document.querySelector(".name");
const description = document.querySelector(".description");

console.log(myName);
console.log(description);

const btn = document.getElementById('dark-mode-btn');

function applyMode(mode) {
    if (mode === 'light') {
        document.body.classList.add('light');
        btn.textContent = 'Dark Mode';
    } else {
        document.body.classList.remove('light');
        btn.textContent = 'Light Mode';
    }
}

applyMode(localStorage.getItem('colorMode') || 'dark');

btn.addEventListener('click', () => {
    const next = (localStorage.getItem('colorMode') || 'dark') === 'dark' ? 'light' : 'dark';
    localStorage.setItem('colorMode', next);
    applyMode(next);
});
