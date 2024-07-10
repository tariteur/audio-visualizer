const canvas = document.getElementById('canvas');
const ctx = canvas.getContext('2d');

canvas.width = window.innerWidth;
canvas.height = window.innerHeight;

const ws = new WebSocket('ws://localhost:8080');

ws.onopen = function() {
    console.log('WebSocket connection established.');
};

let audioArray = [];
let targetAudioArray = [];
let m = 0;
const interpolationDurationUp = 50;
const interpolationDurationDown = 200;
let lastUpdateTime = Date.now();

ws.onmessage = function(event) {
    const message = JSON.parse(event.data);
    const dataObject = JSON.parse(message.data);
    if (message.type === 'audioData') {
        try {
            m = dataObject.length;
            targetAudioArray = dataObject.slice(); // Create a copy of the new audio data

            // Initialize audioArray if it's empty
            if (audioArray.length === 0) {
                audioArray = targetAudioArray.slice();
            }

            lastUpdateTime = Date.now(); // Reset the update time
        } catch (error) {
            console.error('Error processing audioData:', error);
        }
    }
};

ws.onclose = function() {
    console.log('WebSocket connection closed.');
};

function drawBars(m, audioArray) {
    const cellWidth = canvas.width / m;
    const maxHeight = canvas.height;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    for (let i = 0; i < m; ++i) {
        const t = audioArray[i];
        const color = 'red';
        const startPosX = i * cellWidth + cellWidth / 2;
        const startPosY = maxHeight - maxHeight * t;
        const endPosX = startPosX;
        const endPosY = maxHeight;
        const thickness = cellWidth / 1.5 * Math.sqrt(t);

        ctx.beginPath();
        ctx.moveTo(startPosX, startPosY);
        ctx.lineTo(endPosX, endPosY);
        ctx.lineWidth = thickness;
        ctx.strokeStyle = color;
        ctx.stroke();
    }
}

function updateAudioArray() {
    const currentTime = Date.now();
    const timeElapsed = currentTime - lastUpdateTime;

    for (let i = 0; i < audioArray.length; ++i) {
        if (audioArray[i] < targetAudioArray[i]) {
            // Interpolate upwards
            const interpolationFactorUp = Math.min(1, timeElapsed / interpolationDurationUp);
            audioArray[i] += (targetAudioArray[i] - audioArray[i]) * interpolationFactorUp;
            if (audioArray[i] > targetAudioArray[i]) {
                audioArray[i] = targetAudioArray[i];
            }
        } else {
            // Interpolate downwards
            const interpolationFactorDown = Math.min(1, timeElapsed / interpolationDurationDown);
            audioArray[i] -= (audioArray[i] - targetAudioArray[i]) * interpolationFactorDown;
            if (audioArray[i] < targetAudioArray[i]) {
                audioArray[i] = targetAudioArray[i];
            }
        }
    }

    lastUpdateTime = currentTime;
}

function animate() {
    updateAudioArray();
    drawBars(m, audioArray);
    requestAnimationFrame(animate);
}

// Start the animation
animate();
