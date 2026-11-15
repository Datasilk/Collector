import React, { useState, useEffect, useRef, useLayoutEffect } from 'react';

// The Context
const ModalContext = React.createContext({});

// Module-level bridge so non-React code (helpers, services) can also
// show/hide modals and menus without access to React context
const modalHandlers = { show: null, hide: null, showMenu: null, hideMenu: null };

const showModal = (componentOrFunction) => {
    modalHandlers.show?.(componentOrFunction);
};

const hideModal = () => {
    modalHandlers.hide?.();
};

const showMenu = (componentOrFunction, anchorRef, options = {}) => {
    modalHandlers.showMenu?.(componentOrFunction, anchorRef, options);
};

const hideMenu = () => {
    modalHandlers.hideMenu?.();
};

// Renders a menu anchored to an element, positioned outside modal bounds via
// position:fixed and a z-index above the modal layer. Aligns below the anchor
// (options.align = 'bottom', default) or above it ('top'), flipping when the
// menu would be clipped by the viewport edge.
const MenuPopover = ({ menu, onDismiss }) => {
    const menuRef = useRef(null);
    const [pos, setPos] = useState({ left: 0, top: 0, visible: false });

    const updatePosition = () => {
        const el = menuRef.current;
        const anchor = menu.anchorEl;
        if (!el || !anchor || !document.contains(anchor)) return;

        const rect = anchor.getBoundingClientRect();
        const menuRect = el.getBoundingClientRect();
        const align = menu.options.align === 'top' ? 'top' : 'bottom';

        let placement = align;
        if (align === 'bottom' && rect.bottom + menuRect.height > window.innerHeight) {
            placement = 'top';
        } else if (align === 'top' && rect.top - menuRect.height < 0) {
            placement = 'bottom';
        }

        let left = rect.left;
        if (left + menuRect.width > window.innerWidth) {
            left = Math.max(4, window.innerWidth - menuRect.width - 4);
        }

        const top = placement === 'bottom' ? rect.bottom : rect.top - menuRect.height;

        // Only update state when the position actually changed, otherwise
        // this layout effect re-renders forever (maximum update depth exceeded)
        setPos(prev =>
            prev.visible && prev.left === left && prev.top === top
                ? prev
                : { left, top, visible: true }
        );
    };

    useLayoutEffect(() => {
        updatePosition();

        // Reposition when the menu content resizes (e.g. live search results)
        const observer = new ResizeObserver(updatePosition);
        if (menuRef.current) observer.observe(menuRef.current);

        window.addEventListener('resize', updatePosition);
        window.addEventListener('scroll', updatePosition, true);
        return () => {
            observer.disconnect();
            window.removeEventListener('resize', updatePosition);
            window.removeEventListener('scroll', updatePosition, true);
        };
    }, [menu]);

    useEffect(() => {
        const handleClickOutside = (e) => {
            if (menuRef.current?.contains(e.target)) return;
            if (menu.anchorEl?.contains?.(e.target)) return;
            menu.options.onClose?.();
            onDismiss();
        };
        const handleEscape = (e) => {
            if (e.key === 'Escape') {
                menu.options.onClose?.();
                onDismiss();
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        document.addEventListener('keydown', handleEscape);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
            document.removeEventListener('keydown', handleEscape);
        };
    }, [menu, onDismiss]);

    const style = { left: pos.left, top: pos.top, visibility: pos.visible ? 'visible' : 'hidden' };
    if (menu.options.matchWidth && menu.anchorEl) {
        style.minWidth = menu.anchorEl.getBoundingClientRect().width;
    }

    return (
        <div className={'app-menu-popover' + (menu.options.className ? ' ' + menu.options.className : '')} ref={menuRef} style={style}>
            {typeof menu.content === 'function' ? menu.content() : menu.content}
        </div>
    );
};

// Modal Provider
const ModalProvider = ({ children }) => {
    const [modal, setModal] = useState(null);
    const [menu, setMenu] = useState(null);

    const handleShowModal = (componentOrFunction) => {
        // If it's a function, store it as-is so it can be called fresh each render
        // If it's a component, wrap it in a function
        if (typeof componentOrFunction === 'function') {
            setModal(componentOrFunction);
        } else {
            setModal(() => componentOrFunction);
        }
    };

    const handleHideModal = () => {
        setModal(null);
    };

    const handleShowMenu = (componentOrFunction, anchorRef, options = {}) => {
        const anchorEl = anchorRef?.current ?? anchorRef;
        if (!anchorEl) return;
        setMenu({ content: componentOrFunction, anchorEl, options });
    };

    const handleHideMenu = () => {
        setMenu(null);
    };

    // Keep module-level handlers pointing at the live functions
    useEffect(() => {
        modalHandlers.show = handleShowModal;
        modalHandlers.hide = handleHideModal;
        modalHandlers.showMenu = handleShowMenu;
        modalHandlers.hideMenu = handleHideMenu;
    });

    const context = {
        modal,
        showModal: handleShowModal,
        hideModal: handleHideModal,
        menu,
        showMenu: handleShowMenu,
        hideMenu: handleHideMenu
    };

    return (
        <ModalContext.Provider value={context}>
            <>
                {modal ? (typeof modal === 'function' ? modal() : modal) : <></>}
                {menu && <MenuPopover menu={menu} onDismiss={handleHideMenu} />}
                {children}
            </>
            <style>{`
                .app-menu-popover {
                    position: fixed;
                    z-index: 10001;
                }

                .app-menu-popover .entry-type-dropdown {
                    position: static;
                }

                .app-menu-popover .entry-type-dropdown .dropdown-menu {
                    position: static;
                    margin-top: 0;
                }
            `}</style>
        </ModalContext.Provider>
    );
};

// useModal Hook
const useModal = () => {
    const context = React.useContext(ModalContext);
    if (context === undefined) throw new Error('useModal must be used within ModalProvider');
    return context;
};

export {
    ModalProvider,
    useModal,
    showModal,
    hideModal,
    showMenu,
    hideMenu
}
