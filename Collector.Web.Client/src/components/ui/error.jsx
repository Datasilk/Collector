export default function Error({ message, onClose }) {
    return (
        <div className="error-container">
            <div className="error-msg">
                {message}
                <div className="error-close" onClick={onClose}><Icon name="close"></Icon></div>
            </div>
        </div>
    );
}