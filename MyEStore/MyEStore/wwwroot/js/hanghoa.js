// Countdown timer for sales
function createCountdown(endDate, containerSelector) {
    const container = document.querySelector(containerSelector);
    if (!container) return;

    // Create countdown element
    const countdownElement = document.createElement('div');
    countdownElement.className = 'sale-countdown';
    countdownElement.innerHTML = `
    <div class="countdown-title">Ưu đãi kết thúc sau:</div>
    <div class="countdown-timer">
      <div class="countdown-item">
        <span class="countdown-value days">00</span>
        <span class="countdown-label">Ngày</span>
      </div>
      <div class="countdown-item">
        <span class="countdown-value hours">00</span>
        <span class="countdown-label">Giờ</span>
      </div>
      <div class="countdown-item">
        <span class="countdown-value minutes">00</span>
        <span class="countdown-label">Phút</span>
      </div>
      <div class="countdown-item">
        <span class="countdown-value seconds">00</span>
        <span class="countdown-label">Giây</span>
      </div>
    </div>
  `;

    // Add styling
    countdownElement.style.cssText = `
    background: linear-gradient(135deg, #ff6b6b 0%, #e63946 100%);
    color: white;
    padding: 15px;
    border-radius: 10px;
    text-align: center;
    margin-bottom: 20px;
    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
  `;

    // Add styling for countdown timer
    const countdownTimer = countdownElement.querySelector('.countdown-timer');
    countdownTimer.style.cssText = `
    display: flex;
    justify-content: center;
    gap: 15px;
    margin-top: 10px;
  `;

    // Style countdown items
    const countdownItems = countdownElement.querySelectorAll('.countdown-item');
    countdownItems.forEach(item => {
        item.style.cssText = `
      display: flex;
      flex-direction: column;
      align-items: center;
    `;
    });

    // Style countdown values
    const countdownValues = countdownElement.querySelectorAll('.countdown-value');
    countdownValues.forEach(value => {
        value.style.cssText = `
      font-size: 24px;
      font-weight: 700;
      background: rgba(255,255,255,0.2);
      border-radius: 5px;
      padding: 5px 10px;
      min-width: 40px;
      text-align: center;
    `;
    });

    // Style countdown labels
    const countdownLabels = countdownElement.querySelectorAll('.countdown-label');
    countdownLabels.forEach(label => {
        label.style.cssText = `
      font-size: 12px;
      margin-top: 5px;
    `;
    });

    // Insert countdown at the beginning of container
    container.insertBefore(countdownElement, container.firstChild);

    // Update countdown function
    function updateCountdown() {
        const now = new Date().getTime();
        const end = new Date(endDate).getTime();
        const distance = end - now;

        if (distance < 0) {
            countdownElement.innerHTML = `<p class="countdown-ended">Ưu đãi đã kết thúc!</p>`;
            return;
        }

        const days = Math.floor(distance / (1000 * 60 * 60 * 24));
        const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        countdownElement.querySelector('.days').textContent = days.toString().padStart(2, '0');
        countdownElement.querySelector('.hours').textContent = hours.toString().padStart(2, '0');
        countdownElement.querySelector('.minutes').textContent = minutes.toString().padStart(2, '0');
        countdownElement.querySelector('.seconds').textContent = seconds.toString().padStart(2, '0');
    }

    // Update countdown immediately
    updateCountdown();

    // Update countdown every second
    const countdownInterval = setInterval(updateCountdown, 1000);

    // Return cleanup function
    return () => clearInterval(countdownInterval);
}

// Usage example - add this to your main script
document.addEventListener('DOMContentLoaded', () => {
    // Set end date 7 days from today
    const endDate = new Date();
    endDate.setDate(endDate.getDate() + 7);

    // Create countdown timer for products section
    // Replace '.products-section' with the selector of your container
    createCountdown(endDate, '.products-section');
});