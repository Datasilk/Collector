import React, {useState, useEffect} from 'react';
import {getUser, userContext} from './session/user';

// The Context 
const SessionContext = React.createContext({});

// Session Provider
const SessionProvider = ({children}) => {
    // context states
    const [user, setUser] = useState(getUser());
    const [customModulesJournalId, setCustomModulesJournalId] = useState(null);

    // context properties passed to consumer
    const context = {
        ...userContext(user, setUser),
        customModulesJournalId
    };

    //actions
    const handleLogOut = () => {
        context.setUser(null);
    }

    const loadCustomModulesJournal = async () => {
        if (customModulesJournalId) return; // Already loaded
        if (!user) return; // User not logged in
        
        try {
            const { Journals } = await import('@/api/user/journals');
            const { getOrCreateCustomModulesJournal } = Journals(context);
            const response = await getOrCreateCustomModulesJournal();
            
            if (response.data.success) {
                const journal = response.data.data;
                setCustomModulesJournalId(journal.id);
            } else {
                console.error('Failed to load custom modules journal:', response.data.message);
            }
        } catch (err) {
            console.error('Error loading custom modules journal:', err);
        }
    };

    // Load custom modules journal when user logs in
    useEffect(() => {
        if (user && !customModulesJournalId) {
            loadCustomModulesJournal();
        }
    }, [user]);

    context.logout = handleLogOut;

    return (
        <SessionContext.Provider value={context}>
            {children}
        </SessionContext.Provider>
    )
};

// useSession Hook
const useSession = () => {
    const context = React.useContext(SessionContext);
    if(context === undefined) throw new Error('useSession must be used within SessionProvider');
    return context;
};

export {
    SessionProvider,
    useSession
}